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


        public static void RegisterGpsDelegate<T>(this IServiceCollection services) where T : class, IGpsDelegate
            => services.AddSingleton<IGpsDelegate, T>();


        public static bool UseGps(this IServiceCollection builder)
        {
#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<IGpsManager, GpsManagerImpl>();
            return true;
#endif
        }


        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="requestIfAccessAvailable"></param>
        /// <returns></returns>
        public static bool UseGps<T>(this IServiceCollection builder, GpsRequest requestIfAccessAvailable = null) where T : class, IGpsDelegate
        {
#if NETSTANDARD
            return false;
#else
            if (requestIfAccessAvailable != null)
            {
                builder.RegisterPostBuildAction(async sp =>
                {
                    var gps = sp.GetService<IGpsManager>();
                    if (gps.Status == AccessState.Available)
                        await gps.StartListener(requestIfAccessAvailable);
                });
            }
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
