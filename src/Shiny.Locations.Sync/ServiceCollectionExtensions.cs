using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        public static void UseGeofencingSync<T>(this IServiceCollection services, bool requestPermissionOnStart = false)
            where T: class, IGeofenceSyncDelegate
        {
            services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            services.RegisterJob(typeof(SyncGeofenceJob), Constants.GeofenceJobIdentifer, runInForeground: true);
            services.AddSingleton<IGeofenceSyncDelegate, T>();
            services.TryAddSingleton<ILocationSyncManager, LocationSyncManager>();
            services.UseGeofencing<SyncGeofenceDelegate>(requestPermissionOnStart);
        }


        /// <summary>
        /// Unlike geofencing, you have to enable the GPS service via services.UseGps()
        /// </summary>
        /// <typeparam name="IGpsSyncDelegate"></typeparam>
        /// <param name="services"></param>
        public static void UseGpsSync<T>(this IServiceCollection services)
            where T: class, IGpsSyncDelegate
        {
            services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            services.RegisterJob(typeof(SyncGpsJob), Constants.GpsJobIdentifier, runInForeground: true);
            services.TryAddSingleton<ILocationSyncManager, LocationSyncManager>();
            services.AddSingleton<IGpsDelegate, SyncGpsDelegate>();
            services.AddSingleton<IGpsSyncDelegate, T>();
        }
    }
}
