using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Location;
using P = Android.Manifest.Permission;


namespace Shiny.Locations
{
    public class GpsManagerImpl : IGpsManager
    {
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


        public IObservable<IGpsReading> GetLastReading() => Observable.FromAsync(async () =>
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

            var nativeRequest = new LocationRequest()
                .SetPriority(GetPriority(request.Priority));

            if (request.DeferredTime != null)
                nativeRequest.SetInterval(Convert.ToInt64(request.DeferredTime.Value.TotalMilliseconds));

            if (request.DeferredDistance != null)
                nativeRequest.SetSmallestDisplacement((float)request.DeferredDistance.TotalMeters);

            await this.client.RequestLocationUpdatesAsync(
                nativeRequest,
                this.GetPendingIntent()
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
            var intent = new Intent(this.context.AppContext, typeof(GpsBroadcastReceiver));
            intent.SetAction(GpsBroadcastReceiver.INTENT_ACTION);
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