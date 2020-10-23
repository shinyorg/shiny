using System;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGeofenceDelegate : IGeofenceDelegate
    {
        readonly IJobManager jobManager;
        readonly IGeofenceDataService dataService;
        readonly IMotionActivityManager? activityManager;


        public SyncGeofenceDelegate(IJobManager jobManager,
                                    IGeofenceDataService dataService,
                                    IMotionActivityManager? activityManager = null)
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
            if (config.IncludeMotionActivityEvents && this.activityManager != null)
                activity = await this.activityManager.GetCurrentActivity();

            var e = new GeofenceEvent
            {
                Id = Guid.NewGuid().ToString(),
                DateCreated = DateTimeOffset.UtcNow,

                Identifier = region.Identifier,
                Entered = newStatus == GeofenceState.Entered,
                Activities = activity?.Types
            };
            await this.dataService.Create(e);
            if (!this.jobManager.IsRunning)
                await this.jobManager.RunJobAsTask(Constants.GeofenceJobIdentifer);
        }
    }
}
