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
            args => (GeofenceRegionStore)args,
            store => (GeofenceRegion)store);
    }
}
