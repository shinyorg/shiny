using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Locations
{
    public static class Extensions
    {
        /// <summary>
        /// Queries for the most current event
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="maxAge">Timespan containing max age of "current"</param>
        /// <returns></returns>
        public static async Task<MotionActivityEvent> GetCurrentActivity(this IMotionActivity activity, TimeSpan? maxAge = null)
        {
            maxAge = maxAge ?? TimeSpan.FromMinutes(5);
            var end = DateTimeOffset.UtcNow;
            var result = (await activity.Query(end.Subtract(maxAge.Value), end)).OrderBy(x => x.Timestamp).FirstOrDefault();
            return result;
        }


        /// <summary>
        /// Queries for the most current event and checks against type & confidence
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="type"></param>
        /// <param name="maxAge"></param>
        /// <param name="minConfidence"></param>
        /// <returns></returns>
        public static async Task<bool> IsCurrentActivity(this IMotionActivity activity, MotionActivityType type, TimeSpan? maxAge = null, MotionActivityConfidence minConfidence = MotionActivityConfidence.Medium)
        {
            var result = await activity.GetCurrentActivity(maxAge);
            //if (result == default(MotionActivityEvent))
                //return false;
            if (result.Confidence < minConfidence)
                return false;

            return result.Types.HasFlag(type);
        }


        /// <summary>
        /// Queries if most recent activity is automotive
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="maxAge"></param>
        /// <param name="minConfidence"></param>
        /// <returns></returns>
        public static Task<bool> IsCurrentAutomotive(this IMotionActivity activity, TimeSpan? maxAge = null, MotionActivityConfidence minConfidence = MotionActivityConfidence.Medium)
            => activity.IsCurrentActivity(MotionActivityType.Automotive, maxAge, minConfidence);


        /// <summary>
        /// Queries if most recent activity is stationary
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="maxAge"></param>
        /// <param name="minConfidence"></param>
        /// <returns></returns>
        public static Task<bool> IsCurrentStationary(this IMotionActivity activity, TimeSpan? maxAge = null, MotionActivityConfidence minConfidence = MotionActivityConfidence.Medium)
            => activity.IsCurrentActivity(MotionActivityType.Stationary, maxAge, minConfidence);


        /// <summary>
        /// Queries for activities for an entire day (beginning to end)
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public static Task<IList<MotionActivityEvent>> QueryByDate(this IMotionActivity activity, DateTimeOffset date)
            => activity.Query(date.Date, new DateTimeOffset(date.Date.AddDays(1)));


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
