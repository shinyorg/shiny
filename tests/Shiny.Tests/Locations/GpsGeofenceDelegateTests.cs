//#if NETCOREAPP
//using System;
//using System.Threading.Tasks;
//using FluentAssertions;
//using Moq;
//using Xunit;
//using Shiny.Testing.Locations;
//using Shiny.Locations;


//namespace Shiny.Tests.Locations
//{
//    public class GpsGeofenceDelegateTests
//    {
//        readonly GpsGeofenceDelegate gpsDelegate;
//        readonly Mock<IGeofenceManager> geofenceManager;
//        readonly Mock<IGeofenceDelegate> geofenceDelegate;




//        public GpsGeofenceDelegateTests()
//        {
//            this.geofenceManager = new Mock<IGeofenceManager>();
//            this.geofenceManager
//                .Setup(x => x.GetMonitorRegions())
//                .ReturnsAsync(() => new[]
//                {
//                    new GeofenceRegion("test", new Position(1, 1), Distance.FromKilometers(1))
//                });
//            this.geofenceDelegate = new Mock<IGeofenceDelegate>();
//            this.gpsDelegate = new GpsGeofenceDelegate(this.geofenceManager.Object, this.geofenceDelegate.Object);
//        }


//        [Fact]
//        public async Task EntersGeofence()
//        {
//            this.gpsDelegate.CurrentStates.Should().BeEmpty();
//            await this.gpsDelegate.OnReading(GpsReading.Create(1, 1));
//        }
//    }
//}
//#endif