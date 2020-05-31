using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Locations.Sync.Infrastructure
{
    public class SyncGpsDelegate : NotifyPropertyChanged, IGpsDelegate
    {
        readonly IJobManager jobManager;
        readonly IRepository repository;


        public SyncGpsDelegate(IJobManager jobManager, IRepository repository)
        {
            this.jobManager = jobManager;
            this.repository = repository;
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
            await this.repository.Set(e.Id, e);
            this.BatchSize++;

            var job = await this.jobManager.GetJob(Constants.GpsJobIdentifier);
            var config = job.GetParameter<SyncConfig>(Constants.SyncConfigJobParameterKey);

            if (this.BatchSize >= config.BatchSize)
            {
                this.BatchSize = 0;
                Console.WriteLine("GPS Location Sync batch size reached, will attempt to sync");
                if (this.jobManager.IsRunning)
                    await this.jobManager.RunJobAsTask(Constants.GpsJobIdentifier);
            }
        }


        int batchSize;
        public int BatchSize
        {
            get => this.batchSize;
            set => this.Set(ref this.batchSize, value);
        }
    }
}
