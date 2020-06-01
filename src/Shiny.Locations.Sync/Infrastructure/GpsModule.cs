using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


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
            services.AddSingleton<IGpsDelegate, SyncGpsDelegate>();
            services.AddSingleton(typeof(IGpsSyncDelegate), this.delegateType);

            services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            services.RegisterJob(
                typeof(SyncGpsJob),
                Constants.GpsJobIdentifier,
                runInForeground: true,
                parameters: (Constants.SyncConfigJobParameterKey, config ?? new SyncConfig
                {
                    BatchSize = 10,
                    SortMostRecentFirst = false
                })
            );

            if (this.request != null)
                services.UseGps(null, this.request);
        }
    }
}
