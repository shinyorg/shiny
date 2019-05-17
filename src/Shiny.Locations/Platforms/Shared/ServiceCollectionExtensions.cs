using System;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Locations
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseGeofencing<T>(this IServiceCollection builder, params GeofenceRegion[] regionsToRegisterWhenPermissionAvailable) where T : class, IGeofenceDelegate
        {
            builder.AddSingleton<IGeofenceDelegate, T>();
#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<IGeofenceManager, GeofenceManagerImpl>();
            if (regionsToRegisterWhenPermissionAvailable.Any())
            {
                builder.RegisterPostBuildAction(sp =>
                {
                    var mgr = sp.GetService<IGeofenceManager>();
                    mgr
                        .WhenAccessStatusChanged()
                        .Where(x => x == AccessState.Available)
                        .Take(1)
                        .SubscribeAsync(async () =>
                        {
                            foreach (var region in regionsToRegisterWhenPermissionAvailable)
                                await mgr.StartMonitoring(region);
                        });
                });
            }
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
        public static bool UseGps<T>(this IServiceCollection builder, GpsRequest requestWhenPermissionAvailable = null) where T : class, IGpsDelegate
        {
            if (!builder.UseGps())
                return false;

            builder.AddSingleton<IGpsDelegate, T>();
            if (requestWhenPermissionAvailable != null)
            {
                builder.RegisterPostBuildAction(sp =>
                {
                    var gps = sp.GetService<IGpsManager>();
                    gps
                        .WhenAccessStatusChanged(requestWhenPermissionAvailable.UseBackground)
                        .Where(x => x == AccessState.Available)
                        .Take(1)
                        .SubscribeAsync(() => gps.StartListener(requestWhenPermissionAvailable));
                });
            }
            return true;
        }
    }
}
