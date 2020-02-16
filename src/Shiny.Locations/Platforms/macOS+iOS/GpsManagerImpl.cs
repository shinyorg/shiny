using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using CoreLocation;


namespace Shiny.Locations
{
    public class GpsManagerImpl : IGpsManager
    {
        readonly CLLocationManager locationManager;
        readonly GpsManagerDelegate gdelegate;


        public GpsManagerImpl()
        {
            this.gdelegate = new GpsManagerDelegate();
            this.locationManager = new CLLocationManager { Delegate = this.gdelegate };
        }


        public IObservable<AccessState> WhenAccessStatusChanged(GpsRequest request) => this.gdelegate.WhenAccessStatusChanged(request.UseBackground);
        public Task<AccessState> RequestAccess(GpsRequest request) => this.locationManager.RequestAccess(request.UseBackground);
        public AccessState GetCurrentStatus(GpsRequest request) => this.locationManager.GetCurrentStatus(request.UseBackground);
        public bool IsListening { get; private set; }


        public IObservable<IGpsReading> GetLastReading() => Observable.FromAsync(async ct =>
        {
            if (this.locationManager.Location != null)
                return new GpsReading(this.locationManager.Location);

            var task = this
                .WhenReading()
                .Timeout(TimeSpan.FromSeconds(20))
                .Take(1)
                .ToTask(ct);

            var wasListening = this.IsListening;
            try
            {
                if (!wasListening)
                {
                    var access = await this.RequestAccess(new GpsRequest());
                    access.Assert();
                    this.locationManager.StartUpdatingLocation();
                }
                return await task.ConfigureAwait(false);
            }
            finally
            {
                if (!wasListening)
                    this.locationManager.StopUpdatingLocation();
            }
        });


        public async Task StartListener(GpsRequest request)
        {
            if (this.IsListening)
                return;

            request = request ?? new GpsRequest();
            var access = await this.RequestAccess(request);
            access.Assert();

            this.gdelegate.Request = request;
#if __IOS__
            this.locationManager.AllowsBackgroundLocationUpdates = request.UseBackground;
            if (request.ThrottledInterval != null)
                this.locationManager.AllowDeferredLocationUpdatesUntil(0, request.ThrottledInterval.Value.TotalMilliseconds);
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
            this.IsListening = true;
        }


        public Task StopListener()
        {
            if (this.IsListening)
            {
#if __IOS__
                this.locationManager.AllowsBackgroundLocationUpdates = false;
#endif
                this.locationManager.StopUpdatingLocation();
                this.IsListening = false;
            }
            return Task.CompletedTask;
        }


        public IObservable<IGpsReading> WhenReading() => this.gdelegate.WhenGps();
    }
}
