using System;
using System.Threading.Tasks;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGpsDelegate : NotifyPropertyChanged, IGpsDelegate
    {
        readonly IJobManager jobManager;
        readonly IGpsDataService dataService;
        readonly IMotionActivityManager? activityManager;


        public SyncGpsDelegate(IJobManager jobManager,
                               IGpsDataService dataService,
                               IMotionActivityManager? activityManager = null)
        {
            this.jobManager = jobManager;
            this.activityManager = activityManager;
            this.dataService = dataService;
        }


        public async Task OnReading(IGpsReading reading)
        {
            var job = await this.jobManager.GetJob(Constants.GpsJobIdentifier);
            if (job == null)
                return;

            var config = job.GetSyncConfig();
            MotionActivityEvent? activity = null;
            if (config.IncludeMotionActivityEvents && this.activityManager != null)
                activity = await this.activityManager.GetCurrentActivity();

            var e = new GpsEvent
            {
                Id = Guid.NewGuid().ToString(),
                DateCreated = DateTimeOffset.UtcNow,

                Latitude = reading.Position.Latitude,
                Longitude = reading.Position.Longitude,
                Heading = reading.Heading,
                HeadingAccuracy = reading.HeadingAccuracy,
                Speed = reading.Speed,
                PositionAccuracy = reading.PositionAccuracy,
                Activities = activity?.Types
            };
            await this.dataService.Create(e);
            var batchSize = await this.dataService.GetPendingCount();

            if (batchSize >= config.BatchSize)
            {
                Console.WriteLine("GPS Location Sync batch size reached, will attempt to sync");
                if (!this.jobManager.IsRunning)
                    await this.jobManager.RunJobAsTask(Constants.GpsJobIdentifier);
            }
        }
    }
}
