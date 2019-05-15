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


        // TODO: for background, ensure gps delegate is registered
        public Task<AccessState> RequestAccess(bool backgroundMode) => this.locationManager.RequestGpsAccess(backgroundMode);
        public AccessState Status => this.locationManager.CurrentAccessStatus();
        public bool IsListening { get; private set; } // settings?


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
                    var access = await this.RequestAccess(false);
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

            var access = await this.RequestAccess(request.UseBackground);
            access.Assert();

            if (request.DeferredDistanceMeters != null)
                this.locationManager.DistanceFilter = request.DeferredDistanceMeters.Value;

            //this.locationManager.DesiredAccuracy = request
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
                //this.locationManager.AllowsBackgroundLocationUpdates = false;
                this.locationManager.StopUpdatingLocation();
                this.IsListening = false;
            }
            return Task.CompletedTask;
        }


        public IObservable<IGpsReading> WhenReading() => Observable
            .FromEventPattern<CLLocation[]>(
                x => this.gdelegate.UpdatedLocations += x,
                x => this.gdelegate.UpdatedLocations -= x
            )
            .SelectMany(x => x.EventArgs)
            .Select(x => new GpsReading(x));
    }
}
