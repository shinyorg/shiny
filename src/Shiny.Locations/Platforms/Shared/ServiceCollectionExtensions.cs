using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Locations
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseGeofencing<T>(this IServiceCollection builder) where T : class, IGeofenceDelegate
        {
            builder.AddSingleton<IGeofenceDelegate, T>();
            //if (registerGeofences != null)
            //{
            //    builder.OnBuild(async c =>
            //    {
            //        var mgr = c.Resolve<IGeofenceManager>();
            //        var regions = registerGeofences();
            //        // TODO: wait for grant state
            //        foreach (var region in regions)
            //        {
            //            await mgr.StartMonitoring(region);
            //        }
            //    });
            //}
#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<IGeofenceManager, GeofenceManagerImpl>();
            return true;
#endif
        }


        public static bool UseGpsForground(this IServiceCollection builder)
        {
#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<IGpsManager, GpsManagerImpl>();
            return true;
#endif
        }


        public static bool UseGpsBackground<T>(this IServiceCollection builder) where T : class, IGpsDelegate
        {
#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<IGpsDelegate, T>();
            builder.AddSingleton<IGpsManager, GpsManagerImpl>();
            //if (startIfPermissionAvailable)
            //{
            //    builder.RegisterPostBuildAction(async sp =>
            //    {
            //        var access = await sp.GetService<IGpsManager>().RequestAccess(true);
            //        if (access == AccessState.Available)
            //            await gps.StartListener()
            //    });
            //}
            return true;
#endif
        }
    }
}
