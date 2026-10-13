using System;
using System.Threading.Tasks;
using Android.Gms.Location;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores;

namespace Shiny.Locations;


public class GooglePlayServiceGpsManager(
    AndroidPlatform platform,
    [FromKeyedServices(StoreKeys.Default)] IKeyValueStore store,
    ILogger<GooglePlayServiceGpsManager> logger
) : AbstractGpsManager(platform, store, logger)
{
    IFusedLocationProviderClient? listenerClient;

    public override async Task<GpsReading?> GetLastReading(TimeSpan? timeout = null)
    {
        var location = await LocationServices
            .GetFusedLocationProviderClient(this.Platform.AppContext)
            .GetLastLocationAsync()
            .ConfigureAwait(false);

        return location?.FromNative();
    }


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
