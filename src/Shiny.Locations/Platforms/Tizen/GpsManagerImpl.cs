using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Tizen.Location;


namespace Shiny.Locations
{
    //https://developer.tizen.org/development/guides/.net-application/location-and-sensors/location-information
    public class GpsManagerImpl : IGpsManager, IShinyStartupTask
    {
        readonly Locator locator;
        static Location lastKnownLocation = new Location();


        public GpsManagerImpl()
        {
            var locatorType = LocationType.Gps;
            var gps = Platform.Get<bool>("location.gps");
            var wps = Platform.Get<bool>("location.wps");
            if (gps)
            {
                locatorType = wps ? LocationType.Hybrid : LocationType.Gps;
            }
            else
            {
                locatorType = wps ? LocationType.Wps : LocationType.Passive;
            }
            this.locator = new Locator(locatorType);
        }

        public bool IsListening => false;

        public AccessState GetCurrentStatus(GpsRequest request)
        {
            //this.locator.DistanceBasedLocationChanged
            throw new NotImplementedException();
        }

        public IObservable<IGpsReading> GetLastReading()
            => Observable.Empty<IGpsReading>();

        public Task<AccessState> RequestAccess(GpsRequest request)
        {
            throw new NotImplementedException();
        }

        public Task StartListener(GpsRequest request)
        {
            this.locator.Start();
            return Task.CompletedTask;
        }


        public Task StopListener()
        {
            this.locator.Stop();
            return Task.CompletedTask;
        }


        public IObservable<AccessState> WhenAccessStatusChanged(GpsRequest request)
        {
            throw new NotImplementedException();
        }


        public IObservable<IGpsReading> WhenReading() => Observable.Create<IGpsReading>(ob =>
        {
            var handler = new EventHandler<LocationChangedEventArgs>((sender, args) =>
            {

            });
            this.locator.LocationChanged += handler;
            return () => this.locator.LocationChanged -= handler;
        });


        public void Start()
        {
            this.locator.LocationChanged += this.OnLocationChanged;
            this.locator.DistanceBasedLocationChanged += this.OnLocationChanged;
            this.locator.ServiceStateChanged += this.OnServiceStateChanged;
            //this.locator.ZoneChanged
        }

         void OnServiceStateChanged(object sender, ServiceStateChangedEventArgs e)
        {
        }


        void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
        }
    }
}




//            double KmToMetersPerSecond(double km) => km * 0.277778;
//            service.LocationChanged += (s, e) =>
//            {
//                if (e.Location != null)
//                {
//                    lastKnownLocation.Accuracy = e.Location.Accuracy;
//                    lastKnownLocation.Altitude = e.Location.Altitude;
//                    lastKnownLocation.Course = e.Location.Direction;
//                    lastKnownLocation.Latitude = e.Location.Latitude;
//                    lastKnownLocation.Longitude = e.Location.Longitude;
//                    lastKnownLocation.Speed = KmToMetersPerSecond(e.Location.Speed);
//                    lastKnownLocation.Timestamp = e.Location.Timestamp;
//                }
