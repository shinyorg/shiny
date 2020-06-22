using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGeofenceJob : IJob
    {
        readonly ILocationSyncManager syncManager;
        readonly IDataService dataService;
        readonly IGeofenceSyncDelegate? geofences;


        public SyncGeofenceJob(ILocationSyncManager syncManager, 
                               IDataService dataService, 
                               IGeofenceSyncDelegate? geofences = null)
        {
            this.syncManager = syncManager;
            this.dataService = dataService;
            this.geofences = geofences;
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            if (this.geofences == null)
            {
                jobInfo.Repeat = false;
                return false;
            }

            var result = await JobProcessor.Process<GeofenceEvent>(
                this.syncManager,
                jobInfo,
                this.dataService,
                (pings, ct) => this.geofences.Process(pings, ct),
                cancelToken
            );
            return result;
        }
    }
}
