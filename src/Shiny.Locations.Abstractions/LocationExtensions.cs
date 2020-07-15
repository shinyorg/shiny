using System;
using System.Threading.Tasks;


namespace Shiny.Locations
{
    public static class LocationExtensions
    {
        /// <summary>
        /// Determines if the provided position is inside the region.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static bool IsPositionInside(this GeofenceRegion region, Position position)
        {
            var distance = region.Center.GetDistanceTo(position);
            var inside = distance.TotalMeters <= region.Radius.TotalMeters;
            return inside;
        }

        /// <summary>
        /// Requests access for GPS and starts listening for changes.
        /// </summary>
        /// <param name="gps">The gps manager.</param>
        /// <param name="request">The gps request.</param>
        /// <returns></returns>
        public static async Task<AccessState> RequestAccessAndStart(this IGpsManager gps, GpsRequest request)
        {
            var access = await gps.RequestAccess(request);
            if (access == AccessState.Available)
                await gps.StartListener(request);

            return access;
        }
    }
}
