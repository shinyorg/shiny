using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGpsJob : IJob
    {
        readonly IRepository repository;
        readonly IGpsSyncDelegate? gps;


        public SyncGpsJob(IRepository repository, IGpsSyncDelegate? gps = null)
        {
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
                jobInfo, 
                this.repository, 
                pings => this.gps.Process(pings)
            );
            return result;
        }
    }
}
