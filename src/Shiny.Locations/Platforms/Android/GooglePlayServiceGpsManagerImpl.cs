using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Gms.Location;
using Microsoft.Extensions.Logging;


namespace Shiny.Locations
{
    public class GooglePlayServiceGpsManagerImpl : AbstractGpsManager
    {
        FusedLocationProviderClient? client;
        public GooglePlayServiceGpsManagerImpl(IAndroidContext context, ILogger<GooglePlayServiceGpsManagerImpl> logger) : base(context, logger) { }


        public override IObservable<IGpsReading?> GetLastReading() => Observable.FromAsync(async ct =>
        {
            (await this.RequestAccess(GpsRequest.Foreground).ConfigureAwait(false)).Assert(null, true);

            this.client ??= LocationServices.GetFusedLocationProviderClient(this.Context.AppContext);
            var location = await this.client
                .GetLastLocationAsync()
                .ConfigureAwait(false);

            if (location != null)
                return new GpsReading(location);

            return null;
        });


        protected override Task RequestLocationUpdates(GpsRequest request) => LocationServices
            .GetFusedLocationProviderClient(this.Context.AppContext)
            .RequestLocationUpdatesAsync(
                request.ToNative(),
                this.Callback
            );


        protected override async Task RemoveLocationUpdates()
        {
            if (this.client == null)
                return;

            await this.client.RemoveLocationUpdatesAsync(this.Callback);
            this.client = null;
        }
    }
}