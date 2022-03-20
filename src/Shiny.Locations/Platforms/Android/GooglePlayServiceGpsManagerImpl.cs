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
        public GooglePlayServiceGpsManagerImpl(IPlatform context, ILogger<GooglePlayServiceGpsManagerImpl> logger) : base(context, logger) { }


        public override IObservable<IGpsReading?> GetLastReading() => Observable.FromAsync(async ct =>
        {
            (await this.RequestAccess(GpsRequest.Foreground).ConfigureAwait(false)).Assert(null, true);

            var location = await LocationServices
                .GetFusedLocationProviderClient(this.Context.AppContext)
                .GetLastLocationAsync()
                .ConfigureAwait(false);

            if (location != null)
                return new GpsReading(location);

            return null;
        });


        protected override Task RequestLocationUpdates(GpsRequest request) 
        {
            this.listenerClient ??= LocationServices.GetFusedLocationProviderClient(this.Context.AppContext);

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