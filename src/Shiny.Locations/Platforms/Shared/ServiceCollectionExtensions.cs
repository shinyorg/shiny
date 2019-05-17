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
        /// This registers GPS services with the Shiny container as well as the delegate - you can also auto-start the listener when necessary background permissions are received
        /// </summary>
        /// <typeparam name="T">The IGpsDelegate to call</typeparam>
        /// <param name="builder">The servicecollection to configure</param>
        /// <param name="requestIfAccessAvailable">This will be called when permission is given to use GPS functionality (background permission is assumed when calling this - setting your GPS request to not use background is ignored)</param>
        /// <returns></returns>
        public static bool UseGps<T>(this IServiceCollection builder, Action<GpsRequest> requestWhenPermissionAvailable = null) where T : class, IGpsDelegate
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
                        .WhenAccessStatusChanged(true)
                        .Where(x => x == AccessState.Available)
                        .Take(1)
                        .SubscribeAsync(async () =>
                        {
                            var request = new GpsRequest();
                            requestWhenPermissionAvailable(request);
                            request.UseBackground = true;
                            await gps.StartListener(request);
                        });
                });
            }
            return true;
        }
    }
}
