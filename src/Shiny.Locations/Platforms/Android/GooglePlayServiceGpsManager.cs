using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Gms.Location;
using Android.OS;
using Microsoft.Extensions.Logging;

namespace Shiny.Locations;


public class GooglePlayServiceGpsManager(
    AndroidPlatform platform, 
    ILogger<GooglePlayServiceGpsManager> logger
) : AbstractGpsManager(platform, logger)
{
    IFusedLocationProviderClient? listenerClient;

    public override IObservable<GpsReading?> GetLastReading() => Observable.FromAsync(async ct =>
    {
        var location = await LocationServices
            .GetFusedLocationProviderClient(this.Platform.AppContext)
            .GetLastLocationAsync()
            .ConfigureAwait(false);

        return location?.FromNative();
    });


    protected override Task RequestLocationUpdates(AndroidGpsRequest request)
    {
        var builder = new LocationRequest.Builder((int)request.GpsPriority, request.IntervalMillis)
            .SetWaitForAccurateLocation(request.WaitForAccurateLocation)
            .SetGranularity(Granularity.GranularityPermissionLevel);

        if (request.DistanceFilterMeters > 0)
            builder = builder.SetMinUpdateDistanceMeters((float)request.DistanceFilterMeters);

        this.listenerClient ??= LocationServices.GetFusedLocationProviderClient(this.Platform.AppContext);
        return this.listenerClient.RequestLocationUpdatesAsync(
            builder.Build(),
            this.Callback,
            Looper.MainLooper
        );
    }


    protected override async Task RemoveLocationUpdates()
    {
        if (this.listenerClient == null)
            return;

        await this.listenerClient.RemoveLocationUpdatesAsync(this.Callback);
        this.listenerClient = null;
    }
}