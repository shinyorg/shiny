using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGeofenceJob : IJob
    {
        readonly ILocationSyncManager syncManager;
        readonly IGeofenceDataService dataService;
        readonly IGeofenceSyncDelegate? geofences;


        public SyncGeofenceJob(ILocationSyncManager syncManager,
                               IGeofenceDataService dataService,
                               IGeofenceSyncDelegate? geofences = null)
        {
            this.syncManager = syncManager;
            this.dataService = dataService;
            this.geofences = geofences;
        }


        public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            var enabled = await this.syncManager.IsMonitoring(LocationSyncType.Geofence);
            if (!enabled || this.geofences == null)
            {
                jobInfo.Repeat = false;
                return;
            }

            await JobProcessor.Process(
                jobInfo,
                () => this.dataService.GetAll(),
                x => this.dataService.Remove(x),
                (pings, ct) => this.geofences.Process(pings, ct),
                cancelToken
            );
        }
    }
}
