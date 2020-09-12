using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Location;


namespace Shiny.Locations
{
    public class GpsManagerImpl : NotifyPropertyChanged,
                                  IGpsManager,
                                  IGpsBackgroundNotificationConfiguration,
                                  IShinyStartupTask
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


        public async void Start()
        {
            if (this.CurrentListener != null)
            {
                if (this.CurrentListener.UseBackground)
                    await this.StartListenerInternal(this.CurrentListener);
                else
                    this.CurrentListener = null;
            }
        }


        string? title;
        public string? Title
        {
            get => this.title;
            set => this.Set(ref this.title, value);
        }


        string? description;
        public string? Description
        {
            get => this.description;
            set => this.Set(ref this.description, value);
        }


        string? ticker;
        public string? Ticker
        {
            get => this.ticker;
            set => this.Set(ref this.ticker, value);
        }


        GpsRequest? request;
        public GpsRequest? CurrentListener
        {
            get => this.request;
            set => this.Set(ref this.request, value);
        }


        public IObservable<AccessState> WhenAccessStatusChanged(GpsRequest request)
            => Observable.Interval(TimeSpan.FromSeconds(2)).Select(_ => this.GetCurrentStatus(request));

        public AccessState GetCurrentStatus(GpsRequest request)
            => this.context.GetCurrentLocationAccess(request.UseBackground, true, true, false);

        public Task<AccessState> RequestAccess(GpsRequest request)
            => this.context.RequestLocationAccess(request.UseBackground, true, true, false);


        public IObservable<IGpsReading?> GetLastReading() => Observable.FromAsync(async () =>
        {
            var access = await this.RequestAccess(new GpsRequest());
            access.Assert();

            var location = await this.client.GetLastLocationAsync();
            if (location == null)
                return null;

            return new GpsReading(location);
        });


        public async Task StartListener(GpsRequest? request = null)
        {
            if (this.CurrentListener != null)
                return;

            await this.StartListenerInternal(request);
        }


        public async Task StopListener()
        {
            if (this.CurrentListener == null)
                return;

            await this.client.RemoveLocationUpdatesAsync(this.GetPendingIntent());
            this.context.StopService(typeof(ShinyGpsService));
            this.CurrentListener = null;
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


        protected virtual async Task StartListenerInternal(GpsRequest request)
        {
            request = request ?? new GpsRequest();
            var access = await this.RequestAccess(request);
            access.Assert();

            var nativeRequest = LocationRequest
                .Create()
                .SetPriority(GetPriority(request.Priority))
                .SetInterval(request.Interval.ToMillis());

            if (request.ThrottledInterval != null)
                nativeRequest.SetFastestInterval(request.ThrottledInterval.Value.ToMillis());

            if (request.MinimumDistance != null)
                nativeRequest.SetSmallestDisplacement((float)request.MinimumDistance.TotalMeters);

            await this.client.RequestLocationUpdatesAsync(
                nativeRequest,
                this.GetPendingIntent() // used for background - should switch to LocationCallback for foreground
            );
            if (request.UseBackground && !ShinyGpsService.IsStarted)
                this.context.StartService(typeof(ShinyGpsService), true);

            this.CurrentListener = request;
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