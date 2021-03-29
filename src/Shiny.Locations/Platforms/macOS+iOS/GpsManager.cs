using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CoreLocation;
using Microsoft.Extensions.Logging;


namespace Shiny.Locations
{
    public partial class GpsManager : IGpsManager, IShinyStartupTask
    {
        readonly CLLocationManager locationManager;
        readonly GpsManagerDelegate gdelegate;
        readonly ILogger logger;


        public GpsManager(ILogger<IGpsManager> logger)
        {
            this.logger = logger;
            this.gdelegate = new GpsManagerDelegate();
            this.locationManager = new CLLocationManager { Delegate = this.gdelegate };
            //this.locationManager.RequestTemporaryFullAccuracyAuthorizationAsync
            //this.locationManager.RequestTemporaryFullAccuracyAuthorization("purposeKey")
            //this.locationManager.ActivityType = CLActivityType.AutomotiveNavigation
            // iOS 14
            //this.locationManager.AccuracyAuthorization == CLAccuracyAuthorization.FullAccuracy
        }


        public async void Start()
        {
            if (this.CurrentListener != null)
            {
                try
                {
                    if (this.CurrentListener.UseBackground)
                        await this.StartListenerInternal(this.CurrentListener);
                    else
                        this.CurrentListener = null;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error trying to restart GPS");
                }
            }
        }


        public IObservable<AccessState> WhenAccessStatusChanged(GpsRequest request) => this.gdelegate.WhenAccessStatusChanged(request.UseBackground);
        public Task<AccessState> RequestAccess(GpsRequest request) => this.locationManager.RequestAccess(request.UseBackground);
        public AccessState GetCurrentStatus(GpsRequest request) => this.locationManager.GetCurrentStatus(request.UseBackground);


        GpsRequest? request;
        public GpsRequest? CurrentListener
        {
            get => this.request;
            set => this.Set(ref this.request, value);
        }


        public IObservable<IGpsReading> GetLastReading() => Observable.FromAsync(async ct =>
        {
            if (this.locationManager.Location != null)
                return new GpsReading(this.locationManager.Location);

            var task = this
                .WhenReading()
                .Timeout(TimeSpan.FromSeconds(20))
                .Take(1)
                .ToTask(ct);

            var listen = this.CurrentListener;
            try
            {
                if (listen == null)
                {
                    (await this.RequestAccess(new GpsRequest { UseBackground = false })).Assert(null, true);
                    this.locationManager.StartUpdatingLocation();
                }
                return await task.ConfigureAwait(false);
            }
            finally
            {
                if (listen == null)
                    this.locationManager.StopUpdatingLocation();
            }
        });


        public async Task StartListener(GpsRequest request)
        {
            if (this.CurrentListener != null)
                throw new ArgumentException("There is already an active GPS listener");

            await this.StartListenerInternal(request);
        }


        public Task StopListener()
        {
            if (this.CurrentListener != null)
            {
#if __IOS__
                this.locationManager.AllowsBackgroundLocationUpdates = false;
#endif
                this.locationManager.StopUpdatingLocation();
                this.CurrentListener = null;
            }
            return Task.CompletedTask;
        }


        protected virtual async Task StartListenerInternal(GpsRequest request)
        {
            var access = await this.RequestAccess(request);
            access.Assert();
            this.gdelegate.Request = request;
#if __IOS__
            this.locationManager.AllowsBackgroundLocationUpdates = request.UseBackground;
            var throttledInterval = request.ThrottledInterval?.TotalSeconds ?? 0;
            var minDistance = request.MinimumDistance?.TotalMeters ?? 0;

            if (throttledInterval > 0 || minDistance > 0)
            {
                this.locationManager.AllowDeferredLocationUpdatesUntil(
                    minDistance,
                    throttledInterval
                );
            }
#endif
            switch (request.Priority)
            {
                case GpsPriority.Highest:
                    this.locationManager.DesiredAccuracy = CLLocation.AccuracyBest;
                    break;

                case GpsPriority.Normal:
                    this.locationManager.DesiredAccuracy = CLLocation.AccuracyNearestTenMeters;
                    break;

                case GpsPriority.Low:
                    this.locationManager.DesiredAccuracy = CLLocation.AccuracyHundredMeters;
                    break;
            }

            //this.locationManager.ShouldDisplayHeadingCalibration
            //this.locationManager.ShowsBackgroundLocationIndicator
            //this.locationManager.PausesLocationUpdatesAutomatically = false;
            //this.locationManager.DisallowDeferredLocationUpdates
            //this.locationManager.ActivityType = CLActivityType.Airborne;
            //this.locationManager.LocationUpdatesPaused
            //this.locationManager.LocationUpdatesResumed
            //this.locationManager.Failed
            //this.locationManager.UpdatedHeading
            //if (CLLocationManager.HeadingAvailable)
            //    this.locationManager.StopUpdatingHeading();
            this.locationManager.StartUpdatingLocation();
            this.CurrentListener = request;
        }

        public IObservable<IGpsReading> WhenReading() => this.gdelegate.WhenGps();
    }
}
