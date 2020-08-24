using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Locations.Sync;
using Shiny.Locations.Sync.Infrastructure;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register to sync geofence data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="requestPermissionOnStart"></param>
        public static void UseGeofencingSync<T>(this IServiceCollection services)
            where T: class, IGeofenceSyncDelegate
            => services.UseGeofencingSync(typeof(T));


        /// <summary>
        /// Register to sync geofence data
        /// </summary>
        /// <param name="services"></param>
        /// <param name="delegateType"></param>
        /// <param name="config"></param>
        /// <param name="requestPermissionOnStart"></param>
        public static void UseGeofencingSync(this IServiceCollection services, Type delegateType)
            => services.RegisterModule(new GeofenceModule(delegateType));


        /// <summary>
        /// Registers to sync GPS data
        /// </summary>
        /// <typeparam name="IGpsSyncDelegate"></typeparam>
        /// <param name="services"></param>
        public static void UseGpsSync<T>(this IServiceCollection services)
            where T: class, IGpsSyncDelegate => services.UseGpsSync(typeof(T));


        /// <summary>
        /// Registers to sync GPS data
        /// </summary>
        /// <param name="services"></param>
        /// <param name="delegateType"></param>
        /// <param name="request"></param>
        /// <param name="config"></param>
        public static void UseGpsSync(this IServiceCollection services, Type delegateType)
            => services.RegisterModule(new GpsModule(delegateType));
    }
}
