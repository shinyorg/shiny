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
            var events = await this.repository.GetAll<GeofenceEvent>();
            foreach (var e in events)
            {
                //await this.geofences.Process(e);
                await this.repository.Remove<GeofenceEvent>(e.Id);
            }


            throw new NotImplementedException();
        }
    }
}
