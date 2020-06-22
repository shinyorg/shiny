using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;


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
            services.TryAddSingleton<IDataService, SqliteDataService>();
            services.UseGeofencing<SyncGeofenceDelegate>(this.requestPermissionOnStart);

            services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            var job = new JobInfo(typeof(SyncGeofenceJob), Constants.GeofenceJobIdentifer) { RunOnForeground = true };
            job.SetSyncConfig(this.config);
            services.RegisterJob(job);
        }
    }
}
