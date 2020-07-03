using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGpsJob : IJob
    {
        readonly ILocationSyncManager syncManager;
        readonly IDataService dataService;
        readonly IGpsSyncDelegate? gps;


        public SyncGpsJob(ILocationSyncManager syncManager,
                          IDataService dataService, 
                          IGpsSyncDelegate? gps = null)
        {
            this.syncManager = syncManager;
            this.dataService = dataService;
            this.gps = gps;
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            var enabled = await this.syncManager.IsMonitoring(LocationSyncType.GPS);
            if (!enabled || this.gps == null)
            {
                jobInfo.Repeat = false;
                return false;
            }
            var result = await JobProcessor.Process<GpsEvent>(
                jobInfo, 
                this.dataService,
                (pings, ct) => this.gps.Process(pings, ct),
                cancelToken
            );
            return result;
        }
    }
}
