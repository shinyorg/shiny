using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using CoreLocation;
using Shiny.Stores;
using UIKit;

namespace Shiny.Locations;


public class GeofenceManagerImpl : IGeofenceManager
{
    readonly CLLocationManager locationManager;
    readonly Lazy<IEnumerable<IGeofenceDelegate>> delegates;
    readonly IRepository<GeofenceRegion> repository;


    public GeofenceManagerImpl(
        IServiceProvider services,
        IRepository<GeofenceRegion> repository
    )
    {
        this.delegates = services.GetLazyService<IEnumerable<IGeofenceDelegate>>();
        this.repository = repository;
        this.locationManager = new CLLocationManager
        {
            Delegate = new GeofenceManagerDelegate(this)
        };
    }


    readonly Subject<(CLCircularRegion Region, CLRegionState State)> regionSubj = new();
    internal async void OnStateDetermined(CLRegionState state, CLRegion region)
    {
        if (region is CLCircularRegion native)
            this.regionSubj.OnNext((native, state));
    }


    internal async void OnRegionChanged(CLRegion region, bool entered)
    {
        if (region is CLCircularRegion native)
        {
            var geofence = await this.repository
                .Get(native.Identifier)
                .ConfigureAwait(false);

            if (geofence != null)
            {
                var status = entered ? GeofenceState.Entered : GeofenceState.Exited;
                await this.delegates
                    .Value
                    .RunDelegates(x => x.OnStatusChanged(status, geofence))
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


    public Task<AccessState> RequestAccess()
        => this.locationManager.RequestAccess(true);


    public Task<IList<GeofenceRegion>> GetMonitorRegions()
        => this.repository.GetList();


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

        var tcs = new TaskCompletionSource<object>();
        UIApplication.SharedApplication.BeginInvokeOnMainThread(async () =>
        {
            try
            {
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

        await this.repository
            .Set(region)
            .ConfigureAwait(false);
    }


    public async Task StopMonitoring(string identifier)
    {
        var region = await this.repository
            .Get(identifier)
            .ConfigureAwait(false);

        if (region != null)
        {
            await this.repository
                .Remove(region.Identifier)
                .ConfigureAwait(false);

            this.locationManager.StopMonitoring(region.ToNative());
        }
    }


    public async Task StopAllMonitoring()
    {
        await this.repository
            .Clear()
            .ConfigureAwait(false);

        var natives = this
            .locationManager
            .MonitoredRegions
            .OfType<CLCircularRegion>()
            .ToList();

        foreach (var native in natives)
            this.locationManager.StopMonitoring(native);
    }
}