using System;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGeofenceDelegate : IGeofenceDelegate
    {
        readonly IJobManager jobManager;
        readonly IDataService dataService;


        public SyncGeofenceDelegate(IJobManager jobManager, IDataService dataService)
        {
            this.jobManager = jobManager;
            this.dataService = dataService;
        }


        public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
        {
            var e = new GeofenceEvent
            {
                Id = Guid.NewGuid().ToString(),
                DateCreated = DateTimeOffset.UtcNow,

                Identifier = region.Identifier,
                Entered = newStatus == GeofenceState.Entered
            };
            await this.dataService.Create(e);
            if (!this.jobManager.IsRunning)
                await this.jobManager.RunJobAsTask(Constants.GeofenceJobIdentifer);
        }
    }
}
