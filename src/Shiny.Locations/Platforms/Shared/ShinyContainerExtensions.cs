using Shiny.Locations;

namespace Shiny;


public static class ShinyContainerExtensions
{
    public static IGpsManager Gps(this ShinyContainer container) => container.GetService<IGpsManager>();
    public static IGeofenceManager Geofences(this ShinyContainer container) => container.GetService<IGeofenceManager>();
    public static IMotionActivityManager MotionActivity(this ShinyContainer container) => container.GetService<IMotionActivityManager>();
}
