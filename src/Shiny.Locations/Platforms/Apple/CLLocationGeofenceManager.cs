using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
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


    readonly Subject<(CLCircularRegion Region, CLRegionState State)> regionSubj = new();

    internal void OnStateDetermined(CLRegionState state, CLRegion region)
    {
        if (region is CLCircularRegion native)
            this.regionSubj.OnNext((native, state));
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
        => this.repository.GetList<GeofenceRegion>();


    public async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
    {
        (await this.locationManager.RequestAccess(false)).Assert();

        var task = this.regionSubj
            .Where(x => region.Equals(x.Region))
            .Take(1)
            .Select(x => x.State.FromNative())
            .Timeout(TimeSpan.FromSeconds(20))
            .ToTask(cancelToken);

        this.locationManager.RequestState(region.ToNative());
        try
        {
            var result = await task.ConfigureAwait(false);
            return result;
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException("Could not retrieve latest GPS coordinates to be able to determine geofence current state", ex);
        }
    }


    public async Task StartMonitoring(GeofenceRegion region)
    {
        (await this.RequestAccess()).Assert();
        var native = region.ToNative();

        if (OperatingSystem.IsIOSVersionAtLeast(17))
        {
            var condition = new CLCircularGeographicCondition(
                new CLLocationCoordinate2D(region.Center.Latitude, region.Center.Longitude),
                region.Radius.TotalMeters
            );

            // can't set identifier/name of the monitor like suggested in the Apple docs
            var mon = await CLMonitor.RequestMonitorAsync(CLMonitorConfiguration.Create(
                "shiny_geofences", 
                DispatchQueue.MainQueue,
                (monitor, evt) =>
                {
                })
            );
            mon.AddCondition(condition, region.Identifier);
            
            // https://forums.developer.apple.com/forums/thread/768373
            // https://developer.apple.com/documentation/corelocation/monitoring-the-user-s-proximity-to-geographic-regions
            // https://developer.apple.com/documentation/corelocation/clmonitor-2r51v/event
            
            // this.locationManager.StartMonitoring(condition); // doesn't exist
            // mon.Events // doesn't exist
            var session = CLBackgroundActivitySession.Create();
        }

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
            // if (OperatingSystemShim.IsMacCatalystVersionAtLeast(17))
            // {
            //     this.locationManager.RemoveCondition(region.Identifier);
            // }
            // this.locationManager.RemoveCondition()
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