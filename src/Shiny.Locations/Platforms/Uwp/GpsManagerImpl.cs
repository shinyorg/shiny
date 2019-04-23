using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;


namespace Shiny.Locations
{
    public class GpsManagerImpl : IGpsManager
    {
        readonly Geolocator geolocator;
        readonly Subject<IGpsReading> gpsSubject;


        public GpsManagerImpl()
        {
            this.geolocator = new Geolocator();
            this.gpsSubject = new Subject<IGpsReading>();
        }


        public AccessState Status
        {
            get
            {
                switch (this.geolocator.LocationStatus)
                {
                    case PositionStatus.Disabled:
                        return AccessState.Disabled;

                    case PositionStatus.NotAvailable:
                        return AccessState.NotSupported;

                    //case PositionStatus.NotInitialized:
                    case PositionStatus.Ready:
                        return AccessState.Available;

                    case PositionStatus.NoData:
                    case PositionStatus.Initializing:
                    default:
                        return AccessState.Unknown;
                }
            }
        }


        public bool IsListening { get; private set; }


        public IObservable<IGpsReading> GetLastReading() => Observable.FromAsync(async ct =>
        {
            var geolocator = new Geolocator();
            geolocator.AllowFallbackToConsentlessPositions();
            var location = await geolocator
                .GetGeopositionAsync()
                .AsTask(ct);

            if (location?.Coordinate == null)
                return null;

            return new GpsReading(location.Coordinate);
        });


        public Task<AccessState> RequestAccess(bool backgroundMode) => Task.FromResult(AccessState.Available);


        public Task StartListener(GpsRequest request = null)
        {
            if (this.IsListening)
                throw new ArgumentException("GPS is already listening");

            this.IsListening = true;
            this.geolocator.PositionChanged += this.OnPositionChanged;
            //this.geolocator.DesiredAccuracy
            //this.geolocator.DesiredAccuracyInMeters
            //this.geolocator.ReportInterval


            return Task.CompletedTask;
        }

        public Task StopListener()
        {
            if (this.IsListening)
            {
                this.geolocator.PositionChanged -= this.OnPositionChanged;
                this.IsListening = false;
            }
            return Task.CompletedTask;
        }


        public IObservable<IGpsReading> WhenReading() => this.gpsSubject;


        void OnPositionChanged(Geolocator sender, PositionChangedEventArgs args)
            => this.gpsSubject.OnNext(new GpsReading(args.Position.Coordinate));
    }
}
