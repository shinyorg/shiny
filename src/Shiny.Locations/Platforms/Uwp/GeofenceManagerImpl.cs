using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Shiny.Infrastructure;
using Shiny.Locations.Infrastructure;


namespace Shiny.Locations
{
    //https://docs.microsoft.com/en-us/windows/uwp/maps-and-location/set-up-a-geofence
    public class GeofenceManagerImpl : AbstractGeofenceManager
    {
        public GeofenceManagerImpl(IRepository repository) : base(repository) {}


        public override async Task<AccessState> RequestAccess()
        {
            var status = await Geolocator.RequestAccessAsync();
            return status switch
            {
                GeolocationAccessStatus.Denied => AccessState.Denied,
                GeolocationAccessStatus.Allowed => GeofenceMonitor.Current.Status.FromNative(),
                _ => AccessState.Unknown
            };
        }


        public override async Task StartMonitoring(GeofenceRegion region)
        {
            await this.Repository.Set(region.Identifier, region).ConfigureAwait(false);
            GeofenceMonitor.Current.Geofences.Add(this.ToNative(region));

            //this.context.RegisterBackground<GeofenceBackgroundTaskProcessor>(
            //    nameof(GeofenceBackgroundTaskProcessor),
            //    builder => builder.SetTrigger(new LocationTrigger(LocationTriggerType.Geofence))
            //);
        }


        public override async Task StopMonitoring(string identifier)
        {
            var list = GeofenceMonitor.Current.Geofences;
            var geofence = list.FirstOrDefault(x => x.Id.Equals(identifier));

            if (geofence != null)
                list.Remove(geofence);

            await this.Repository.Remove(identifier).ConfigureAwait(false);
            //if (list.Count == 0)
            //    this.context.UnRegisterBackground<GeofenceBackgroundTaskProcessor>(nameof(GeofenceBackgroundTaskProcessor));
        }


        public override async Task StopAllMonitoring()
        {
            await this.Repository.Clear().ConfigureAwait(false);
            GeofenceMonitor.Current.Geofences.Clear();
            //this.context.UnRegisterBackground<GeofenceBackgroundTaskProcessor>(nameof(GeofenceBackgroundTaskProcessor));
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
                region.Identifier,
                circle,
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
