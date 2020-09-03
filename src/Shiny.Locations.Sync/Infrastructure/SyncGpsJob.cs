using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGpsJob : IJob
    {
        readonly ILocationSyncManager syncManager;
        readonly IGpsDataService dataService;
        readonly IGpsSyncDelegate? gps;


        public SyncGpsJob(ILocationSyncManager syncManager,
                          IGpsDataService dataService,
                          IGpsSyncDelegate? gps = null)
        {
            this.syncManager = syncManager;
            this.dataService = dataService;
            this.gps = gps;
        }


        public async Task Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            var enabled = await this.syncManager.IsMonitoring(LocationSyncType.GPS);
            if (!enabled || this.gps == null)
                return;

            await JobProcessor.Process(
                jobInfo,
                () => this.dataService.GetAll(),
                x => this.dataService.Remove(x),
                (pings, ct) => this.gps.Process(pings, ct),
                cancelToken
            );
        }
    }
}
