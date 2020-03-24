using System;
using System.Threading.Tasks;
using Moq;
using Shiny.Locations;
using Shiny.Testing.Infrastucture;
using Shiny.Testing.Locations;
using Xunit;


namespace Shiny.Tests.Locations
{
    public class GpsGeofenceManagerImplTests
    {
        readonly GpsGeofenceManagerImpl manager;
        readonly TestGpsManager gps;
        readonly InMemoryRepository repository;


        public GpsGeofenceManagerImplTests()
        {
            this.repository = new InMemoryRepository();
            this.gps = new TestGpsManager();
            this.manager = new GpsGeofenceManagerImpl(
                this.repository,
                this.gps
            );
        }


        [Fact]
        public async Task EntersGeofence()
        {
            await this.manager.StartMonitoring(new GeofenceRegion(
                "Test",
                new Position(1, 1),
                Distance.FromMeters(1)
            ));
            this.gps.PingPosition(1, 1);

            // TODO: tap into delegate
        }
    }
}
