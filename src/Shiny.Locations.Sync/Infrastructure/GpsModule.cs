using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;
using Shiny.Locations.Sync.Infrastructure.Sqlite;

namespace Shiny.Locations.Sync.Infrastructure
{
    public class GpsModule : ShinyModule
    {
        readonly Type delegateType;
        public GpsModule(Type delegateType) => this.delegateType = delegateType;


        public override void Register(IServiceCollection services)
        {
            services.TryAddSingleton<ILocationSyncManager, LocationSyncManager>();
            services.TryAddSingleton<IGpsDataService, GpsDataService>();
            services.AddSingleton<IGpsDelegate, SyncGpsDelegate>();
            services.AddSingleton(typeof(IGpsSyncDelegate), this.delegateType);
            services.UseMotionActivity();
            services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            services.RegisterJob(
                typeof(SyncGpsJob),
                Constants.GpsJobIdentifier,
                InternetAccess.Any,
                true
            );
        }
    }
}
