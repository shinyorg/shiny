using System;
using System.Threading.Tasks;
using Moq;
using Shiny.Locations;
using Shiny.Testing.Infrastucture;
using Shiny.Testing.Locations;
using Xunit;


namespace Shiny.Locations.Tests
{
    public class GpsGeofenceDelegateTests
    {
        readonly GpsGeofenceDelegate gpsDelegate;
        readonly Mock<IGeofenceManager> geofenceManager;
        readonly Mock<IGeofenceDelegate> geofenceDelegate;



        public GpsGeofenceDelegateTests()
        {
            this.geofenceManager = new Mock<IGeofenceManager>();
            this.geofenceManager
                .Setup(x => x.GetMonitorRegions())
                .ReturnsAsync(() => new[]
                {
                    new GeofenceRegion("", null, null)
                });
            this.geofenceDelegate = new Mock<IGeofenceDelegate>();
            this.gpsDelegate = new GpsGeofenceDelegate(this.geofenceManager.Object, this.geofenceDelegate.Object);
        }


        [Fact]
        public async Task EntersGeofence()
        {
            await this.gpsDelegate.OnReading(new GpsReading());

        }
    }
}
