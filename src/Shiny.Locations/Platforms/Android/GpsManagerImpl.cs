using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Location;
using P = Android.Manifest.Permission;


namespace Shiny.Locations
{
    public class GpsManagerImpl : IGpsManager
    {
        public const string ReceiverName = "com.shiny.locations." + nameof(GpsBroadcastReceiver);
        public const string IntentAction = ReceiverName + ".INTENT_ACTION";
        readonly AndroidContext context;
        readonly FusedLocationProviderClient client;


        public GpsManagerImpl(AndroidContext context)
        {
            this.context = context;
            this.client = LocationServices.GetFusedLocationProviderClient(this.context.AppContext);
        }


        public IObservable<AccessState> WhenAccessStatusChanged(bool forBackground) => Observable.Return(AccessState.Available); // TODO
        public AccessState GetCurrentStatus(bool background) => this.context.GetCurrentAccessState(P.AccessFineLocation);
        public bool IsListening { get; private set; }


        public IObservable<IGpsReading?> GetLastReading() => Observable.FromAsync(async () =>
        {
            var access = await this.RequestAccess(false);
            access.Assert();

            var location = await this.client.GetLastLocationAsync();
            if (location == null)
                return null;

            return new GpsReading(location);
        });


        public Task<AccessState> RequestAccess(bool backgroundMode)
            => this.context.RequestAccess(P.AccessFineLocation).ToTask();


        public async Task StartListener(GpsRequest request)
        {
            if (this.IsListening)
                return;

            request = request ?? new GpsRequest();
            var access = await this.RequestAccess(request.UseBackground);
            access.Assert();

            var nativeRequest = LocationRequest
                .Create()
                .SetPriority(GetPriority(request.Priority))
                .SetInterval(request.Interval.ToMillis());

            if (request.ThrottledInterval != null)
                nativeRequest.SetFastestInterval(request.ThrottledInterval.Value.ToMillis());

            await this.client.RequestLocationUpdatesAsync(
                nativeRequest,
                this.GetPendingIntent() // used for background - should switch to LocationCallback for foreground
            );
            this.IsListening = true;
        }


        public async Task StopListener()
        {
            if (!this.IsListening)
                return;

            await this.client.RemoveLocationUpdatesAsync(this.GetPendingIntent());
            this.IsListening = false;
        }


        public IObservable<IGpsReading> WhenReading()
            => GpsBroadcastReceiver.WhenReading();


        protected virtual PendingIntent GetPendingIntent()
        {
            var intent = this.context.CreateIntent<GpsBroadcastReceiver>(IntentAction);
            return PendingIntent.GetBroadcast(
                this.context.AppContext,
                0,
                intent,
                PendingIntentFlags.UpdateCurrent
            );
        }


        protected static int GetPriority(GpsPriority priority)
        {
            switch (priority)
            {
                case GpsPriority.Low:
                    return LocationRequest.PriorityLowPower;

                case GpsPriority.Highest:
                    return LocationRequest.PriorityHighAccuracy;

                case GpsPriority.Normal:
                default:
                    return LocationRequest.PriorityBalancedPowerAccuracy;
            }
        }
    }
}