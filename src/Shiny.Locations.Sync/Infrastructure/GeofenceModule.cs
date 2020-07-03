using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class GeofenceModule : ShinyModule
    {
        readonly Type delegateType;


        public GeofenceModule(Type delegateType)
        {
            this.delegateType = delegateType;
        }


        public override void Register(IServiceCollection services)
        {
            services.AddSingleton(typeof(IGeofenceSyncDelegate), this.delegateType);
            services.TryAddSingleton<ILocationSyncManager, LocationSyncManager>();
            services.TryAddSingleton<IDataService, SqliteDataService>();
            services.UseGeofencing<SyncGeofenceDelegate>();
            services.UseMotionActivity();

            //services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            //var job = new JobInfo(typeof(SyncGeofenceJob), Constants.GeofenceJobIdentifer) { RunOnForeground = true };
            //services.RegisterJob(job);
        }
    }
}
