using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;


namespace Shiny.Locations
{
    public partial class GpsManager: IGpsManager
    {
        readonly Geolocator geolocator;
        readonly Subject<IGpsReading> gpsSubject;


        public GpsManager()
        {
            this.geolocator = new Geolocator();
            this.gpsSubject = new Subject<IGpsReading>();
        }


        public Task<AccessState> RequestAccess(GpsRequest request) => Task.FromResult(this.GetCurrentStatus(request));

        GpsRequest? request;
        public GpsRequest? CurrentListener
        {
            get => this.request;
            set
            {
                var bg = value?.BackgroundMode ?? GpsBackgroundMode.None;
                if (bg == GpsBackgroundMode.None)
                    this.request = value;
                else
                    this.Set(ref this.request, value);
            }
        }


        public AccessState GetCurrentStatus(GpsRequest request) => this.geolocator.LocationStatus switch
        {
            PositionStatus.Disabled => AccessState.Disabled,
            PositionStatus.NotAvailable => AccessState.NotSupported,
            PositionStatus.Ready => AccessState.Available,
            //PositionStatus.NoData, PositionStatus.Initializing:
            _ => AccessState.Unknown
        };


        public IObservable<AccessState> WhenAccessStatusChanged(GpsRequest request) => Observable.Create<AccessState>(ob =>
        {
            var handler = new TypedEventHandler<Geolocator, StatusChangedEventArgs>((sender, args) =>
                ob.OnNext(this.GetCurrentStatus(request))
            );
            this.geolocator.StatusChanged += handler;
            return () => this.geolocator.StatusChanged -= handler;
        });


        public IObservable<IGpsReading?> GetLastReading() => Observable.FromAsync(async ct =>
        {
            var geolocator = new Geolocator();
            geolocator.AllowFallbackToConsentlessPositions();
            var location = await geolocator
                .GetGeopositionAsync()
                .AsTask(ct)
                .ConfigureAwait(false);

            if (location?.Coordinate == null)
                return null;

            return new GpsReading(location.Coordinate);
        });


        public Task StartListener(GpsRequest? request = null)
        {
            if (this.CurrentListener != null)
                throw new InvalidOperationException("Already running GPS");
            
            this.CurrentListener = request ?? new GpsRequest();

            //this.geolocator.DesiredAccuracy = PositionAccuracy.High
            this.geolocator.DesiredAccuracyInMeters = this.CurrentListener.Accuracy switch
            {
                GpsAccuracy.Highest => 0,
                GpsAccuracy.High => 10,
                GpsAccuracy.Normal => 100,
                GpsAccuracy.Low => 1000,
                GpsAccuracy.Lowest => 3000
            };
            //this.geolocator.ReportInterval = Convert.ToUInt32(request.Interval.TotalMilliseconds);
            this.geolocator.PositionChanged += this.OnPositionChanged;
            
            return Task.CompletedTask;
        }


        public Task StopListener()
        {
            if (this.CurrentListener != null)
            {
                this.geolocator.PositionChanged -= this.OnPositionChanged;
                this.CurrentListener = null;
            }
            return Task.CompletedTask;
        }


        public IObservable<IGpsReading> WhenReading() => this.gpsSubject;


        void OnPositionChanged(Geolocator sender, PositionChangedEventArgs args)
            => this.gpsSubject.OnNext(new GpsReading(args.Position.Coordinate));
    }
}
