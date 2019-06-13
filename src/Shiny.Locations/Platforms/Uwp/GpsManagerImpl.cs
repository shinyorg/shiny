using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;


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


        public AccessState GetCurrentStatus(bool background)
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


        public IObservable<AccessState> WhenAccessStatusChanged(bool forBackground) => Observable.Create<AccessState>(ob =>
        {
            var handler = new TypedEventHandler<Geolocator, StatusChangedEventArgs>((sender, args) =>
                ob.OnNext(this.GetCurrentStatus(true))
            );
            this.geolocator.StatusChanged += null;
            return () => this.geolocator.StatusChanged -= null;
        });


        public Task<AccessState> RequestAccess(bool background) => Task.FromResult(this.GetCurrentStatus(background));

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


        public Task StartListener(GpsRequest request = null)
        {
            if (!this.IsListening)
            {
                this.IsListening = true;
                request = request ?? new GpsRequest();

                this.geolocator.ReportInterval = Convert.ToUInt32(request.Interval.TotalMilliseconds);
                this.geolocator.PositionChanged += this.OnPositionChanged;
            }
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
