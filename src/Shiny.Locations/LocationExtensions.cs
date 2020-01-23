using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Locations
{
    public static class LocationExtensions
    {
        public static bool IsPositionInside(this GeofenceRegion region, Position position)
        {
            var distance = region.Center.GetDistanceTo(position);
            var inside = distance.TotalMeters <= region.Radius.TotalMeters;
            return inside;
        }


        public static async Task<AccessState> RequestAccessAndStart(this IGpsManager gps, GpsRequest request)
        {
            var access = await gps.RequestAccess(request.UseBackground);
            if (access == AccessState.Available)
                await gps.StartListener(request);

            return access;
        }


        internal static RepositoryWrapper<GeofenceRegion, GeofenceRegionStore> Wrap(this IRepository repository) => new RepositoryWrapper<GeofenceRegion, GeofenceRegionStore>
        (
            repository,
            args => new GeofenceRegionStore
            {
                Identifier = args.Identifier,
                CenterLatitude = args.Center.Latitude,
                CenterLongitude = args.Center.Longitude,
                RadiusMeters = args.Radius.TotalMeters,
                SingleUse = args.SingleUse,
                NotifyOnEntry = args.NotifyOnEntry,
                NotifyOnExit = args.NotifyOnExit,
                Payload = args.Payload,
            },
            store => new GeofenceRegion(
                store.Identifier,
                new Position(store.CenterLatitude, store.CenterLongitude),
                Distance.FromMeters(store.RadiusMeters)
            )
            {
                SingleUse = store.SingleUse,
                NotifyOnEntry = store.NotifyOnEntry,
                NotifyOnExit = store.NotifyOnExit,
                Payload = store.Payload,
            }
        );
    }
}
