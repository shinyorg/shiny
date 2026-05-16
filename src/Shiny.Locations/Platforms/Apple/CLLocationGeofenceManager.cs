using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreFoundation;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;
using CoreLocation;

namespace Shiny.Locations;


public class CLLocationGeofenceManager : IGeofenceManager
{
    readonly CLLocationManager locationManager;
    readonly IPlatform platform;
    readonly IServiceProvider services;
    readonly ILogger logger;
    readonly IRepository repository;


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
    }


    readonly System.Collections.Concurrent.ConcurrentDictionary<string, TaskCompletionSource<(CLCircularRegion Region, CLRegionState State)>> regionStateTcs = new();

    internal void OnStateDetermined(CLRegionState state, CLRegion region)
    {
        if (region is CLCircularRegion native && this.regionStateTcs.TryGetValue(native.Identifier, out var tcs))
            tcs.TrySetResult((native, state));
    }


    internal async void OnRegionChanged(CLRegion region, bool entered)
    {
        if (region is CLCircularRegion native)
        {
            var geofence = this.repository.Get<GeofenceRegion>(native.Identifier);

            if (geofence != null)
            {
                var status = entered ? GeofenceState.Entered : GeofenceState.Exited;
                await this.services
                    .RunDelegates<IGeofenceDelegate>(
                        x => x.OnStatusChanged(status, geofence),
                        this.logger
                    )
                    .ConfigureAwait(false);

                if (geofence.SingleUse)
                {
                    await this
                        .StopMonitoring(geofence.Identifier)
                        .ConfigureAwait(false);
                }
            }
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
    }


    public Task StopMonitoring(string identifier)
    {
        var region = this.repository.Get<GeofenceRegion>(identifier);

        if (region != null)
        {
            this.repository.Remove<GeofenceRegion>(region.Identifier);
            this.locationManager.StopMonitoring(region.ToNative());
        }
        return Task.CompletedTask;
    }


    public Task StopAllMonitoring()
    {
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
