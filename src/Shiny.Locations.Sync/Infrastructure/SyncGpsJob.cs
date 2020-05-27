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
            // TODO: what if there are items to be sync'd but no sync delegates?
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
            var events = await this.repository.GetAll<GpsEvent>();
            foreach (var e in events)
            {
                //await this.geofences.Process(e);
                await this.repository.Remove<GpsEvent>(e.Id);
            }

            return true;
        }
    }
}
