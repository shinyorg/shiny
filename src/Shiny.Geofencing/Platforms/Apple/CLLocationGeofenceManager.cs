using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores.Repositories;
using Shiny.Hosting;
using CoreLocation;

namespace Shiny.Locations;


public class CLLocationGeofenceManager : IGeofenceManager, IShinyStartupTask, IIosLifecycle.IApplicationLifecycle
{
    readonly CLLocationManager locationManager;
    readonly IPlatform platform;
    readonly IServiceProvider services;
    readonly ILogger logger;
    readonly IRepository repository;
    readonly GeofenceDwellTracker dwell;


    public CLLocationGeofenceManager(
        IPlatform platform,
        IServiceProvider services,
        IRepository repository,
        ILogger<IGeofenceManager> logger
    )
    {
        this.platform = platform;
        this.services = services;
        this.repository = repository;
        this.logger = logger;
        this.locationManager = new CLLocationManager
        {
            Delegate = new GeofenceManagerDelegate(this)
        };

        this.dwell = new GeofenceDwellTracker(
            repository,
            logger,
            this.RequestState,
            r => this.FireDelegate(r, GeofenceState.Dwelling)
        );
    }


    public void Start()
    {
        try
        {
            this.dwell.Restore();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to restore pending geofence dwells");
        }
    }


    readonly System.Collections.Concurrent.ConcurrentDictionary<string, TaskCompletionSource<(CLCircularRegion Region, CLRegionState State)>> regionStateTcs = new();

    public void OnForeground()
    {
        try
        {
            // dwell timers don't advance while iOS has the app suspended - catch up on any that came due
            this.dwell.Reevaluate();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to re-evaluate geofence dwells");
        }
    }


    public void OnBackground() { }


    internal void OnStateDetermined(CLRegionState state, CLRegion region)
    {
        if (region is CLCircularRegion native)
        {
            if (this.regionStateTcs.TryGetValue(native.Identifier, out var tcs))
                tcs.TrySetResult((native, state));

            // starts the dwell clock for a region the device was already inside when monitoring began
            // (a stay already being timed is kept) - never reported to the delegate as an entry
            if (state == CLRegionState.Inside)
            {
                var geofence = this.repository.Get<GeofenceRegion>(native.Identifier);
                if (geofence?.DwellTime != null)
                    this.dwell.Entered(geofence);
            }
        }
    }


    internal async void OnRegionChanged(CLRegion region, bool entered)
    {
        try
        {
            if (region is CLCircularRegion native)
            {
                var geofence = this.repository.Get<GeofenceRegion>(native.Identifier);

                if (geofence != null)
                {
                    // the native region also watches entry/exit when only a dwell was asked for - filter by the flags here
                    if (entered)
                    {
                        this.dwell.Entered(geofence);
                        if (geofence.NotifyOnEntry)
                            await this.FireDelegate(geofence, GeofenceState.Entered).ConfigureAwait(false);
                    }
                    else
                    {
                        // reports Dwelling first if the stay lasted the dwell time
                        await this.dwell.Exited(geofence).ConfigureAwait(false);

                        // a single-use region is gone once its dwell fired
                        if (geofence.NotifyOnExit && this.repository.Exists<GeofenceRegion>(geofence.Identifier))
                            await this.FireDelegate(geofence, GeofenceState.Exited).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error handling geofence event for {Identifier}", region.Identifier);
        }
    }


    async Task FireDelegate(GeofenceRegion geofence, GeofenceState status)
    {
        await this.services
            .RunDelegates<IGeofenceDelegate>(
                x => x.OnStatusChanged(status, geofence),
                this.logger
            )
            .ConfigureAwait(false);

        if (geofence.IsSingleUseComplete(status))
        {
            await this
                .StopMonitoring(geofence.Identifier)
                .ConfigureAwait(false);
        }
    }

    public AccessState CurrentStatus
        => this.locationManager.GetCurrentStatus(true);

    public Task<AccessState> RequestAccess()
        => this.locationManager.RequestAccess(true);

    public IList<GeofenceRegion> GetMonitorRegions()
        => this.repository.GetAll<GeofenceRegion>().ToList();


    public async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
    {
        var tcs = new TaskCompletionSource<(CLCircularRegion, CLRegionState)>();
        this.regionStateTcs[region.Identifier] = tcs;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        cts.CancelAfter(TimeSpan.FromSeconds(20));
        cts.Token.Register(() => tcs.TrySetException(
            new TimeoutException("Could not retrieve latest GPS coordinates to be able to determine geofence current state")
        ));

        this.locationManager.RequestState(region.ToNative());
        try
        {
            var result = await tcs.Task.ConfigureAwait(false);
            return result.Item2.FromNative();
        }
        finally
        {
            this.regionStateTcs.TryRemove(region.Identifier, out _);
        }
    }


    public async Task StartMonitoring(GeofenceRegion region)
    {
        var native = region.ToNative();

        var tcs = new TaskCompletionSource<object?>();
        this.platform.InvokeOnMainThread(() =>
        {
            try
            {
                // CLMonitor
                // CLCircularRegion
                this.locationManager.StartMonitoring(native);
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                this.locationManager.StopMonitoring(native);
                tcs.SetException(ex);
            }
        });
        await tcs.Task.ConfigureAwait(false);

        this.repository.Set(region);

        // no entry event comes for a region the device is already inside - ask, so a dwell can start timing
        if (region.DwellTime != null)
            this.platform.InvokeOnMainThread(() => this.locationManager.RequestState(native));
    }


    public Task StopMonitoring(string identifier)
    {
        var region = this.repository.Get<GeofenceRegion>(identifier);

        this.dwell.Remove(identifier);
        if (region != null)
        {
            this.repository.Remove<GeofenceRegion>(region.Identifier);
            this.locationManager.StopMonitoring(region.ToNative());
        }
        return Task.CompletedTask;
    }


    public Task StopAllMonitoring()
    {
        this.dwell.Clear();
        this.repository.Clear<GeofenceRegion>();

        var natives = this
            .locationManager
            .MonitoredRegions
            .OfType<CLCircularRegion>()
            .ToList();

        foreach (var native in natives)
            this.locationManager.StopMonitoring(native);

        return Task.CompletedTask;
    }
}
