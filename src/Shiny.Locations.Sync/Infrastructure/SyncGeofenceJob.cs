using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGeofenceJob : IJob
    {
        readonly IRepository repository;
        readonly IGeofenceSyncDelegate? geofences;


        public SyncGeofenceJob(IRepository repository, IGeofenceSyncDelegate? geofences = null)
        {
            this.repository = repository;
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
                jobInfo,
                this.repository,
                (pings, ct) => this.geofences.Process(pings, ct),
                cancelToken
            );
            return result;
        }
    }
}
