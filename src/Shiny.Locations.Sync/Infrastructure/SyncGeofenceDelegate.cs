using System;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGeofenceDelegate : IGeofenceDelegate
    {
        readonly IJobManager jobManager;
        readonly IMotionActivityManager activityManager;
        readonly IDataService dataService;


        public SyncGeofenceDelegate(IJobManager jobManager, 
                                    IMotionActivityManager activityManager,
                                    IDataService dataService)
        {
            this.jobManager = jobManager;
            this.activityManager = activityManager;
            this.dataService = dataService;
        }


        public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
        {
            var job = await this.jobManager.GetJob(Constants.GeofenceJobIdentifer);
            if (job == null)
                return;

            var config = job.GetSyncConfig();
            MotionActivityEvent? activity = null;
            if (config.IncludeMotionActivityEvents)
                activity = await this.activityManager.GetCurrentActivity();

            var e = new GeofenceEvent
            {
                Id = Guid.NewGuid().ToString(),
                DateCreated = DateTimeOffset.UtcNow,

                Identifier = region.Identifier,
                Entered = newStatus == GeofenceState.Entered,
                Activities = activity
            };
            await this.dataService.Create(e);
            if (!this.jobManager.IsRunning)
                await this.jobManager.RunJobAsTask(Constants.GeofenceJobIdentifer);
        }
    }
}
