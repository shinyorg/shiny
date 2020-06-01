using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Locations;
using Shiny.Locations.Sync;
using Shiny.Locations.Sync.Infrastructure;


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
        public static void UseGeofencingSync<T>(this IServiceCollection services, SyncConfig? config = null, bool requestPermissionOnStart = false)
            where T: class, IGeofenceSyncDelegate
            => services.UseGeofencingSync(typeof(T), config, requestPermissionOnStart);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="delegateType"></param>
        /// <param name="config"></param>
        /// <param name="requestPermissionOnStart"></param>
        public static void UseGeofencingSync(this IServiceCollection services, Type delegateType, SyncConfig? config = null, bool requestPermissionOnStart = false)
            => services.RegisterModule(new GeofenceModule(delegateType, requestPermissionOnStart, config));


        /// <summary>
        /// Unlike geofencing, you have to enable the GPS service via services.UseGps()
        /// </summary>
        /// <typeparam name="IGpsSyncDelegate"></typeparam>
        /// <param name="services"></param>
        public static void UseGpsSync<T>(this IServiceCollection services, GpsRequest? request = null, SyncConfig? config = null)
            where T: class, IGpsSyncDelegate => services.UseGpsSync(typeof(T), request, config);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="delegateType"></param>
        /// <param name="request"></param>
        /// <param name="config"></param>
        public static void UseGpsSync(this IServiceCollection services, Type delegateType, GpsRequest? request = null, SyncConfig? config = null)
            => services.RegisterModule(new GpsModule(delegateType, request, config));
    }
}
