using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class GeofenceModule : ShinyModule
    {
        readonly Type delegateType;
        readonly SyncConfig config;
        readonly bool requestPermissionOnStart;


        public GeofenceModule(Type delegateType, bool requestPermissionOnStart, SyncConfig? config)
        {
            this.delegateType = delegateType;
            this.requestPermissionOnStart = requestPermissionOnStart;

            this.config = config ?? new SyncConfig
            {
                BatchSize = 1,
                SortMostRecentFirst = false
            };
        }


        public override void Register(IServiceCollection services)
        {
            services.AddSingleton(typeof(IGeofenceSyncDelegate), this.delegateType);
            services.TryAddSingleton<ILocationSyncManager, LocationSyncManager>();
            services.UseGeofencing<SyncGeofenceDelegate>(this.requestPermissionOnStart);
            services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            services.RegisterJob(
                typeof(SyncGeofenceJob),
                Constants.GeofenceJobIdentifer,
                runInForeground: true,
                parameters: (Constants.SyncConfigJobParameterKey, this.config)
            );
        }
    }
}
