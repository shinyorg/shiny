using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;
using Shiny.Locations;


namespace Shiny.Locations.Sync
{
    public class SyncLocationDelegate : IGpsDelegate, IGeofenceDelegate
    {
        readonly IJobManager jobManager;
        readonly IRepository repository;


        public SyncLocationDelegate(IJobManager jobManager, IRepository repository)
        {
            this.jobManager = jobManager;
            this.repository = repository;
        }


        public async Task OnReading(IGpsReading reading)
        {
            //await this.repository.Set()
            await this.jobManager.RunJobAsTask("");
        }


        public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
        {
            //await this.repository.Set()
            await this.jobManager.RunJobAsTask("");
        }
    }
}
