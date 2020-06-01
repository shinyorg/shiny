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
        public static void UseGeofencingSync<T>(this IServiceCollection services, SyncConfig? config = null, bool requestPermissionOnStart = false)
            where T: class, IGeofenceSyncDelegate
        {
            services.AddSingleton<IGeofenceSyncDelegate, T>();
            services.TryAddSingleton<ILocationSyncManager, LocationSyncManager>();
            services.UseGeofencing<SyncGeofenceDelegate>(requestPermissionOnStart);
            services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            services.RegisterJob(
                typeof(SyncGeofenceJob), 
                Constants.GeofenceJobIdentifer, 
                runInForeground: true,
                parameters: ("Config", config ?? new SyncConfig
                {
                    BatchSize = 1,
                    SortMostRecentFirst = true
                })
            );
        }


        /// <summary>
        /// Unlike geofencing, you have to enable the GPS service via services.UseGps()
        /// </summary>
        /// <typeparam name="IGpsSyncDelegate"></typeparam>
        /// <param name="services"></param>
        public static void UseGpsSync<T>(this IServiceCollection services, GpsRequest? request = null, SyncConfig? config = null)
            where T: class, IGpsSyncDelegate
        {
            services.TryAddSingleton<ILocationSyncManager, LocationSyncManager>();
            services.AddSingleton<IGpsDelegate, SyncGpsDelegate>();
            services.AddSingleton<IGpsSyncDelegate, T>();

            services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            services.RegisterJob(
                typeof(SyncGpsJob), 
                Constants.GpsJobIdentifier, 
                runInForeground: true,
                parameters: ("Config", config ?? new SyncConfig 
                { 
                    BatchSize = 10,
                    SortMostRecentFirst = false
                })
            );

            if (request != null)
            {
                services.UseGps(request);
            }
        }
    }
}
