using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


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
            if (!this.jobManager.IsRunning)
                await this.jobManager.RunJobAsTask(Constants.GpsJobIdentifier);
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
