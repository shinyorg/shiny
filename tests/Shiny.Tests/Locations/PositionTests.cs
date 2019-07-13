using System;
using Shiny.Locations;
using FluentAssertions;
using Xunit;


namespace Shiny.Tests.Locations
{
    public class PositionTests
    {
        [Fact]
        public void Inside_Geofence()
        {
            var current = new Position(43.6429228, -79.3789959); // union station
            var center = new Position(43.6411314, -79.3808415); // 88 queen's quay
            var distance = center.GetDistanceTo(current);

            var region = new GeofenceRegion("test", center, Distance.FromKilometers(3));
            Assert.True(distance < Distance.FromKilometers(1), "Union station is less than a 1000 meters away");
            region
                .IsPositionInside(current)
                .Should()
                .Be(true, "Union station is inside the 3km geofence from 88 Queen's Quay");
        }


        [Fact]
        public void Outside_Geofence()
        {
            var center = new Position(43.6411314, -79.3808415); // 88 queen's quay
            var current = new Position(43.6515754, -79.3492364); // random point outside fence
            var region = new GeofenceRegion("test", center, Distance.FromKilometers(2));

            region
                .IsPositionInside(current)
                .Should()
                .Be(false);
        }
    }
}
