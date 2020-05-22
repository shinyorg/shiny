using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Locations;
using Shiny.Locations.Sync;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="requestPermissionOnStart"></param>
        public static void UseGeofenceSync<T>(this IServiceCollection services, bool requestPermissionOnStart = false)
            where T: class, IGeofenceSyncDelegate
        {
            // TODO: register config options
            services.RegisterJob(typeof(SyncJob));
            services.AddSingleton<IGeofenceSyncDelegate, T>();
            services.UseGeofencing<SyncLocationDelegate>(requestPermissionOnStart);
        }


        /// <summary>
        /// Unlike geofencing, you have to enable the GPS service via services.UseGps()
        /// </summary>
        /// <typeparam name="IGpsSyncDelegate"></typeparam>
        /// <param name="services"></param>
        public static void UseGpsSync<T>(this IServiceCollection services)
            where T: class, IGpsSyncDelegate
        {
            //services.RegisterJob(typeof(SyncJob));
            services.AddSingleton<IGpsDelegate, SyncLocationDelegate>();
            services.AddSingleton<IGpsSyncDelegate, T>();
        }
    }
}
