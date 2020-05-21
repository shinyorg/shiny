using System;
using System.Threading;
using System.Threading.Tasks;

using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Locations.Sync
{
    public class SyncJob : IJob
    {
        readonly IRepository repository;
        readonly SyncConfig config;
        readonly IGeofenceSyncDelegate? geofences;
        readonly IGpsSyncDelegate? gps;


        public SyncJob(IRepository repository,
                       SyncConfig config, // TODO: gps & geofence needs one of these though
                       IGeofenceSyncDelegate? geofences = null,
                       IGpsSyncDelegate? gps = null)
        {
            // TODO: what if there are items to be sync'd but no sync delegates?
            this.geofences = geofences;
            this.gps = gps;
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }


        async Task DoGps()
        {
        }


        async Task DoGeofences()
        {
        }
    }
}
