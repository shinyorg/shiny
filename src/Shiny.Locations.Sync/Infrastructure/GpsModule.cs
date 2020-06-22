using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class GpsModule : ShinyModule
    {
        readonly Type delegateType;
        readonly GpsRequest? request;
        readonly SyncConfig config;


        public GpsModule(Type delegateType, GpsRequest? request, SyncConfig? config)
        {
            this.delegateType = delegateType;
            this.request = request;
            this.config = config ?? new SyncConfig
            {
                BatchSize = 10,
                SortMostRecentFirst = false
            };
        }


        public override void Register(IServiceCollection services)
        {
            services.TryAddSingleton<ILocationSyncManager, LocationSyncManager>();
            services.TryAddSingleton<IDataService, SqliteDataService>();
            services.AddSingleton<IGpsDelegate, SyncGpsDelegate>();
            services.AddSingleton(typeof(IGpsSyncDelegate), this.delegateType);

            services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            var job = new JobInfo(typeof(SyncGpsJob), Constants.GpsJobIdentifier) { RunOnForeground = true };
            job.SetSyncConfig(this.config);
            services.RegisterJob(job);

            if (this.request != null)
                services.UseGps(null, this.request);
        }
    }
}
