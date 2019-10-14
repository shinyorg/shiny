using System;
using Shiny.Locations;


namespace Shiny
{
    public static class CrossGps
    {
        public static IGpsManager Current => ShinyHost.Resolve<IGpsManager>();
    }


    public static class CrossGeofences
    {
        public static IGeofenceManager Current => ShinyHost.Resolve<IGeofenceManager>();
    }


    public static class CrossMotionActivity
    {
        public static IMotionActivityManager Current => ShinyHost.Resolve<IMotionActivityManager>();
    }
}
