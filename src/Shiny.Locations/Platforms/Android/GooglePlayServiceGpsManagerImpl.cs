using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Gms.Location;


namespace Shiny.Locations
{
    public class GooglePlayServiceGpsManagerImpl : AbstractGpsManager
    {
        //public const string ReceiverName = "com.shiny.locations." + nameof(GpsBroadcastReceiver);
        //public const string IntentAction = ReceiverName + ".INTENT_ACTION";
        readonly FusedLocationProviderClient client;


        public GooglePlayServiceGpsManagerImpl(IAndroidContext context) : base(context)
        {
            this.client = LocationServices.GetFusedLocationProviderClient(context.AppContext);
        }

         
        public override IObservable<IGpsReading?> GetLastReading() => Observable.FromAsync(async () =>
        {
            (await this.RequestAccess(GpsRequest.Default)).Assert();

            var location = await this.client.GetLastLocationAsync();
            if (location == null)
                return null;

            return new GpsReading(location);
        });


        //public IObservable<IGpsReading> WhenReading()
        //    => GpsBroadcastReceiver.WhenReading();


        //protected virtual PendingIntent GetPendingIntent()
        //{
        //    var intent = this.context.CreateIntent<GpsBroadcastReceiver>(IntentAction);
        //    return PendingIntent.GetBroadcast(
        //        this.context.AppContext,
        //        0,
        //        intent,
        //        PendingIntentFlags.UpdateCurrent
        //    );
        //}

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