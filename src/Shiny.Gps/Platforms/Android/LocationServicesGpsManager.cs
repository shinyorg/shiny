using System;
using System.Threading.Tasks;
using Android.Locations;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores;
using AContext = Android.Content.Context;

namespace Shiny.Locations;


public class LocationServicesGpsManager : AbstractGpsManager
{
    readonly LocationManager client;
    public LocationServicesGpsManager(
        AndroidPlatform platform,
        [FromKeyedServices(StoreKeys.Default)] IKeyValueStore store,
        ILogger<LocationServicesGpsManager> logger
    ) : base(platform, store, logger)
        => this.client = platform.GetSystemService<LocationManager>(AContext.LocationService);


    public override Task<GpsReading?> GetLastReading(TimeSpan? timeout = null)
    {
        var criteria = new Criteria
        {
            BearingRequired = false,
            AltitudeRequired = false,
            SpeedRequired = false
        };
        // Accuracy - Coarse, Fine, Low, Medium, High, NoRequirement
        //criteria.Accuracy = Accuracy.Coarse
        //criteria.HorizontalAccuracy = Accuracy.Coarse
        //SpeedRequired
        //criteria.SpeedAccuracy = Accuracy.Coarse;
        //BearingRequired
        //criteria.BearingAccuracy = Accuracy.Coarse;
        //criteria.VerticalAccuracy = Accuracy.Coarse
        var location = this.client.GetLastKnownLocation(this.client.GetBestProvider(criteria, true));
        return Task.FromResult(location?.FromNative());
    }


    protected override Task RequestLocationUpdates(AndroidGpsRequest request)
    {
        var criteria = new Criteria
        {
            BearingRequired = true,
            AltitudeRequired = true,
            SpeedRequired = true
        };

        this.client.RequestLocationUpdates(
            1000,
            (float)request.DistanceFilterMeters,
            criteria,
            this.Callback,
            Looper.MainLooper
        );
        return Task.CompletedTask;
    }


    protected override Task RemoveLocationUpdates()
    {
        this.client.RemoveUpdates(this.Callback);
        return Task.CompletedTask;
    }
}
