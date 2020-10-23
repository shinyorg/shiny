using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;
using Shiny.Locations.Sync.Infrastructure.Sqlite;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class GeofenceModule : ShinyModule
    {
        readonly Type delegateType;
        public GeofenceModule(Type delegateType) => this.delegateType = delegateType;


        public override void Register(IServiceCollection services)
        {
            if (!services.UseGeofencing<SyncGeofenceDelegate>())
            {
                Logging.Log.Write("LocationSync", "Geofencing could not be registered");
                return;
            }
            services.UseMotionActivity();
            services.AddSingleton(typeof(IGeofenceSyncDelegate), this.delegateType);
            services.TryAddSingleton<SyncSqliteConnection>();
            services.TryAddSingleton<ILocationSyncManager, LocationSyncManager>();
            services.TryAddSingleton<IGeofenceDataService, GeofenceDataService>();
            services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            services.RegisterJob(
                typeof(SyncGeofenceJob),
                Constants.GeofenceJobIdentifer,
                InternetAccess.Any,
                true
            );
        }
    }
}
