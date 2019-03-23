using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Shiny.Infrastructure;


namespace Shiny.Locations
{
    //https://docs.microsoft.com/en-us/windows/uwp/maps-and-location/set-up-a-geofence
    public class GeofenceManagerImpl : AbstractGeofenceManager
    {
        public GeofenceManagerImpl(IRepository repository) : base(repository) {}


        public override async Task<AccessState> RequestAccess()
        {
            var status = await Geolocator.RequestAccessAsync();
            switch (status)
            {
                case GeolocationAccessStatus.Denied:
                    return AccessState.Denied;

                case GeolocationAccessStatus.Allowed:
                    //return AccessState.Available;
                    return this.Status;

                case GeolocationAccessStatus.Unspecified:
                default:
                    return AccessState.Unknown;
            }
        }


        public override AccessState Status
        {
            get
            {
                switch (GeofenceMonitor.Current.Status)
                {
                    case GeofenceMonitorStatus.Ready:
                        return AccessState.Available;

                    case GeofenceMonitorStatus.Disabled:
                        return AccessState.Disabled;

                    case GeofenceMonitorStatus.NotAvailable:
                        return AccessState.NotSupported;

                    default:
                        return AccessState.Unknown;
                }
            }
        }


        public override async Task StartMonitoring(GeofenceRegion region)
        {
            await this.Repository.Set(region.Identifier, region);
            var native = this.ToNative(region);
            GeofenceMonitor.Current.Geofences.Add(native);
        }


        public override async Task StopMonitoring(GeofenceRegion region)
        {
            var list = GeofenceMonitor.Current.Geofences;
            var geofence = list.FirstOrDefault(x => x.Id.Equals(region.Identifier));

            if (geofence != null)
                list.Remove(geofence);

            await this.Repository.Remove(region.Identifier);
        }


        public override async Task StopAllMonitoring()
        {
            await this.Repository.Clear();
            GeofenceMonitor.Current.Geofences.Clear();
        }


        public override Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken)
        {
            var native = GeofenceMonitor.Current;
            var coords = native.LastKnownGeoposition?.Coordinate?.Point?.Position;
            if (coords == null)
                return Task.FromResult(GeofenceState.Unknown);

            var position = new Position(coords.Value.Latitude, coords.Value.Longitude);
            var result = region.IsPositionInside(position)
                ? GeofenceState.Entered
                : GeofenceState.Exited;

            return Task.FromResult(result);
        }


        Geofence ToNative(GeofenceRegion region)
        {
            var position = new BasicGeoposition
            {
                Latitude = region.Center.Latitude,
                Longitude = region.Center.Longitude
            };

            var circle = new Geocircle(position, region.Radius.TotalMeters);
            var geofence = new Geofence(
                region.Identifier, circle,
                ToStates(region),
                region.SingleUse
            );
            return geofence;
        }


        static MonitoredGeofenceStates ToStates(GeofenceRegion region)
        {
            var i = 0;
            if (region.NotifyOnEntry)
                i = 1;
            if (region.NotifyOnExit)
                i += 2;
            return (MonitoredGeofenceStates) i;
        }
    }
}
