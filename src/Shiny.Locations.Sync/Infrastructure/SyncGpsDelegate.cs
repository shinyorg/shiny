using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGpsDelegate : NotifyPropertyChanged, IGpsDelegate
    {
        readonly IJobManager jobManager;
        readonly IDataService dataService;


        public SyncGpsDelegate(IJobManager jobManager, IDataService dataService)
        {
            this.jobManager = jobManager;
            this.dataService = dataService;
        }


        public async Task OnReading(IGpsReading reading)
        {
            var e = new GpsEvent
            {
                Id = Guid.NewGuid().ToString(),
                DateCreated = DateTimeOffset.UtcNow,

                Latitude = reading.Position.Latitude,
                Longitude = reading.Position.Longitude,
                Heading = reading.Heading,
                HeadingAccuracy = reading.HeadingAccuracy,
                Speed = reading.Speed,
                PositionAccuracy = reading.PositionAccuracy
            };
            await this.dataService.Create(e);
            var batchSize = await this.dataService.GetPendingCount<GeofenceEvent>();

            var job = await this.jobManager.GetJob(Constants.GpsJobIdentifier);
            var config = job.GetSyncConfig();

            if (batchSize >= config.BatchSize)
            {
                Console.WriteLine("GPS Location Sync batch size reached, will attempt to sync");
                if (!this.jobManager.IsRunning)
                    await this.jobManager.RunJobAsTask(Constants.GpsJobIdentifier);
            }
        }
    }
}
