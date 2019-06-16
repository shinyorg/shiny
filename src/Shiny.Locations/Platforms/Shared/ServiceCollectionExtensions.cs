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

#if WINDOWS_UWP
            builder.AddSingleton<IBackgroundTaskProcessor, GeofenceBackgroundTaskProcessor>();
#endif

#if NETSTANDARD
            return false;
#else
            builder.AddSingleton<IGeofenceManager, GeofenceManagerImpl>();
            if (regionsToRegisterWhenPermissionAvailable.Any())
            {
                builder.RegisterPostBuildAction(async sp =>
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

                    await mgr.RequestAccess();
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
        /// <param name="requestIfPermissionGranted">This will be called when permission is given to use GPS functionality (background permission is assumed when calling this - setting your GPS request to not use background is ignored)</param>
        /// <returns></returns>
        public static bool UseGps<T>(this IServiceCollection builder, Action<GpsRequest> requestIfPermissionGranted = null) where T : class, IGpsDelegate
        {
            if (!builder.UseGps())
                return false;

            builder.AddSingleton<IGpsDelegate, T>();
            if (requestIfPermissionGranted != null)
            {
                builder.RegisterPostBuildAction(async sp =>
                {
                    var request = new GpsRequest();
                    requestIfPermissionGranted(request);
                    request.UseBackground = true;

                    var mgr = sp.GetService<IGpsManager>();
                    mgr
                        .WhenAccessStatusChanged(true)
                        .Where(x => x == AccessState.Available)
                        .Take(1)
                        .SubscribeAsync(() => mgr.StartListener(request));

                    await mgr.RequestAccess(true);
                });
#endif
            }
            return true;
        }
    }
}
