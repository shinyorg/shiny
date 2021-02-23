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
            set => this.Set(ref this.request, value);
        }


        public AccessState GetCurrentStatus(GpsRequest request)
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


        public IObservable<AccessState> WhenAccessStatusChanged(GpsRequest request) => Observable.Create<AccessState>(ob =>
        {
            var handler = new TypedEventHandler<Geolocator, StatusChangedEventArgs>((sender, args) =>
                ob.OnNext(this.GetCurrentStatus(request))
            );
            this.geolocator.StatusChanged += null;
            return () => this.geolocator.StatusChanged -= null;
        });




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


        public Task StartListener(GpsRequest? request = null)
        {
            if (this.CurrentListener != null)
            {
                this.CurrentListener = request ?? new GpsRequest();

                this.geolocator.ReportInterval = Convert.ToUInt32(request.Interval.TotalMilliseconds);
                this.geolocator.PositionChanged += this.OnPositionChanged;
            }
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
