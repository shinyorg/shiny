using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;

namespace Shiny.Locations
{
    public static class LocationExtensions
    {
        //https://stackoverflow.com/questions/2042599/direction-between-2-latitude-longitude-points-in-c-sharp
        public static double GetCompassBearingTo(this Position from, Position to)
        {
            var dLon = ToRad(to.Longitude - from.Longitude);
            var dPhi = Math.Log(Math.Tan(ToRad(to.Latitude) / 2 + Math.PI / 4) / Math.Tan(ToRad(from.Latitude) / 2 + Math.PI / 4));
            if (Math.Abs(dLon) > Math.PI)
                dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);

            return ToBearing(Math.Atan2(dLon, dPhi));
        }


        public static double ToRad(double degrees)
            => degrees * (Math.PI / 180);

        public static double ToDegrees(double radians)
            => radians * 180 / Math.PI;

        public static double ToBearing(double radians)
            => (ToDegrees(radians) + 360) % 360;


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
                NotifyOnExit = args.NotifyOnExit
            },
            store => new GeofenceRegion(
                store.Identifier,
                new Position(store.CenterLatitude, store.CenterLongitude),
                Distance.FromMeters(store.RadiusMeters)
            )
            {
                SingleUse = store.SingleUse,
                NotifyOnEntry = store.NotifyOnEntry,
                NotifyOnExit = store.NotifyOnExit
            }
        );
    }
}
