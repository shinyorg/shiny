using System;
using System.Threading.Tasks;


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
            var access = await gps.RequestAccess(request);
            if (access == AccessState.Available)
                await gps.StartListener(request);

            return access;
        }
    }
}
