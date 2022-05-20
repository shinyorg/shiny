using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Gms.Location;
using Android.OS;

using Microsoft.Extensions.Logging;


namespace Shiny.Locations
{
    public class GooglePlayServiceGpsManagerImpl : AbstractGpsManager
    {
        FusedLocationProviderClient? listenerClient;
        readonly AndroidPlatform platform;

        public GooglePlayServiceGpsManagerImpl(AndroidPlatform platform, ILogger<GooglePlayServiceGpsManagerImpl> logger) : base(platform, logger)
        { }


        public override IObservable<GpsReading?> GetLastReading() => Observable.FromAsync(async ct =>
        {
            (await this.RequestAccess(GpsRequest.Foreground).ConfigureAwait(false)).Assert(null, true);

            var location = await LocationServices
                .GetFusedLocationProviderClient(this.Platform.AppContext)
                .GetLastLocationAsync()
                .ConfigureAwait(false);

            return location?.FromNative();
        });


        protected override Task RequestLocationUpdates(GpsRequest request)
        {
            this.listenerClient ??= LocationServices.GetFusedLocationProviderClient(this.Platform.AppContext);

            return this.listenerClient.RequestLocationUpdatesAsync(
                request.ToNative(),
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
}