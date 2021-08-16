using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Gms.Location;
using Microsoft.Extensions.Logging;


namespace Shiny.Locations
{
    public class GooglePlayServiceGpsManagerImpl : AbstractGpsManager
    {
        readonly FusedLocationProviderClient client;
        public GooglePlayServiceGpsManagerImpl(IAndroidContext context, ILogger<GooglePlayServiceGpsManagerImpl> logger) : base(context, logger)
            => this.client = LocationServices.GetFusedLocationProviderClient(context.AppContext);


        public override IObservable<IGpsReading?> GetLastReading() => Observable.FromAsync(async ct =>
        {
            (await this.RequestAccess(GpsRequest.Foreground)).Assert(null, true);

            var location = await this.client.GetLastLocationAsync();
            if (location != null)
                return new GpsReading(location);

            return null;
        });


        protected override async Task RequestLocationUpdates(GpsRequest request)
        {
            var nativeRequest = request.ToNative();
            await this.client.RequestLocationUpdatesAsync(
                nativeRequest,
                this.Callback
            );
        }


        protected override Task RemoveLocationUpdates()
            => this.client.RemoveLocationUpdatesAsync(this.Callback);
    }
}