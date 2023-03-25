using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Locations;
using Android.OS;
using Microsoft.Extensions.Logging;
using AContext = Android.Content.Context;

namespace Shiny.Locations;


public class LocationServicesGpsManager : AbstractGpsManager
{
    readonly LocationManager client;
    public LocationServicesGpsManager(AndroidPlatform platform, ILogger<LocationServicesGpsManager> logger) : base(platform, logger)
        => this.client = platform.GetSystemService<LocationManager>(AContext.LocationService);


    public override IObservable<GpsReading?> GetLastReading() => Observable.FromAsync(async ct =>
    {
        (await this.RequestAccess(GpsRequest.Foreground)).Assert(null, true);

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
        return location?.FromNative();
    });


    protected override Task RequestLocationUpdates(GpsRequest request)
    {
        var criteria = new Criteria
        {
            BearingRequired = true,
            AltitudeRequired = true,
            SpeedRequired = true
        };

        this.client.RequestLocationUpdates(
            0,
            request.Accuracy switch
            {
                GpsAccuracy.Highest => 0F,
                GpsAccuracy.High => 10F,
                GpsAccuracy.Normal => 100F,
                GpsAccuracy.Low => 1000F,
                GpsAccuracy.Lowest => 3000F
            },
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
