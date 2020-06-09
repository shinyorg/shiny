using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGpsJob : IJob
    {
        readonly ILocationSyncManager syncManager;
        readonly IRepository repository;
        readonly IGpsSyncDelegate? gps;


        public SyncGpsJob(ILocationSyncManager syncManager, 
                          IRepository repository, 
                          IGpsSyncDelegate? gps = null)
        {
            this.syncManager = syncManager;
            this.repository = repository;
            this.gps = gps;
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            if (this.gps == null)
            {
                jobInfo.Repeat = false;
                return false;
            }
            var result = await JobProcessor.Process<GpsEvent>(
                this.syncManager,
                jobInfo, 
                this.repository,
                (pings, ct) => this.gps.Process(pings, ct),
                cancelToken
            );
            return result;
        }
    }
}
