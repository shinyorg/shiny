using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Locations;

[assembly: LocationsAutoRegisterAttribute]

namespace Shiny.Locations
{
    public class LocationsAutoRegisterAttribute : AutoRegisterAttribute
    {
        public override void Register(IServiceCollection services)
        {
            services.UseGps();
            //services.UseGeofencing<>
            //services.UseMotionActivity();
        }
    }


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
