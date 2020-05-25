using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        public static void UseGeofencingSync<T>(this IServiceCollection services, bool requestPermissionOnStart = false)
            where T: class, IGeofenceSyncDelegate
        {
            RegJob(services, Constants.GeofenceJobIdentifer);
            services.AddSingleton<IGeofenceSyncDelegate, T>();
            services.TryAddSingleton<ILocationSyncManager, LocationSyncManager>();
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
            RegJob(services, Constants.GpsJobIdentifier);
            services.TryAddSingleton<ILocationSyncManager, LocationSyncManager>();
            services.AddSingleton<IGpsDelegate, SyncLocationDelegate>();
            services.AddSingleton<IGpsSyncDelegate, T>();
        }


        static void RegJob(IServiceCollection services, string identifier)
        {
            services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            services.RegisterJob(typeof(SyncJob), identifier, runInForeground: true);
        }
    }
}
