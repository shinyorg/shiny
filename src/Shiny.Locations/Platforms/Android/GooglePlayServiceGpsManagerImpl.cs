using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.Gms.Location;


namespace Shiny.Locations
{
    public class GooglePlayServiceGpsManagerImpl : AbstractGpsManager
    {
        readonly FusedLocationProviderClient client;
        public GooglePlayServiceGpsManagerImpl(IAndroidContext context) : base(context)
            => this.client = LocationServices.GetFusedLocationProviderClient(context.AppContext);


        public override IObservable<IGpsReading?> GetLastReading() => Observable.FromAsync(async ct =>
        {
            (await this.RequestAccess(GpsRequest.Foreground)).Assert(null, true);

            var location = await this.client.GetLastLocationAsync();
            if (location != null)
                return new GpsReading(location);

            try
            {
                var task = this.WhenReading().Take(1).ToTask(ct);
                await this.RequestLocationUpdates(GpsRequest.Foreground);
                var reading = await task.ConfigureAwait(false);
                return reading;
            }
            finally
            {
                await this.RemoveLocationUpdates();
            }
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