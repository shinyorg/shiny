using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGeofenceDelegate : IGeofenceDelegate
    {
        readonly IJobManager jobManager;
        readonly IRepository repository;


        public SyncGeofenceDelegate(IJobManager jobManager, IRepository repository)
        {
            this.jobManager = jobManager;
            this.repository = repository;
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
            await this.repository.Set(e.Id, e);
            if (!this.jobManager.IsRunning)
                await this.jobManager.RunJobAsTask(Constants.GeofenceJobIdentifer);
        }
    }
}
