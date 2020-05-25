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
        readonly IGeofenceSyncDelegate? geofences;
        readonly IGpsSyncDelegate? gps;


        public SyncJob(IRepository repository,
                       IGeofenceSyncDelegate? geofences = null,
                       IGpsSyncDelegate? gps = null)
        {
            // TODO: what if there are items to be sync'd but no sync delegates?
            this.repository = repository;
            this.geofences = geofences;
            this.gps = gps;
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            // TODO: only send the latest?
            // TODO: expiry on items?
            // TODO: ship sequentially or in batches?
                // TODO: ship the config throught he job info parameters
            switch (jobInfo.Identifier)
            {
                case Constants.GeofenceJobIdentifer:
                    break;

                case Constants.GpsJobIdentifier:
                    break;
            }
            return true;
        }


        async Task DoGeofences()
        {
            var events = await this.repository.GetAll<GeofenceEvent>();
            foreach (var e in events)
            {
                //this.geofences.Process(e)
            }
        }


        async Task DoGps()
        {
            var events = await this.repository.GetAll<GpsEvent>();

        }
    }
}
