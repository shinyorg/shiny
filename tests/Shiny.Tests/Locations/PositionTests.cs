using Shiny.Locations;

namespace Shiny.Tests.Locations;


public class PositionTests
{
    const double lat0 = 43.6429228;
    const double lng0 = -79.3789959;
    const double lat1 = 43.6411314;
    const double lng1 = -79.3808415;

    [Fact]
    public void Inside_Geofence()
    {
        var current = new Position(lat0, lng0); // union station
        var center = new Position(lat1, lng1); // 88 queen's quay
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
        var center = new Position(lat1, lng1); // 88 queen's quay
        var current = new Position(43.6515754, -79.3492364); // random point outside fence
        var region = new GeofenceRegion("test", center, Distance.FromKilometers(2));

        region
            .IsPositionInside(current)
            .Should()
            .Be(false);
    }


    [Theory]
    [InlineData(lat0, lng0, lat0, lng0, true)]
    [InlineData(lat0, lng0, lat1, lng0, false)]
    [InlineData(lat0, lng0, lat0, lng1, false)]
    [InlineData(lat0, lng0, lat1, lng1, false)]
    public void EqualsPosition(double lat1, double lng1, double lat2, double lng2, bool expected) =>
        new Position(lat1, lng1)
            .Equals(new Position(lat2, lng2))
            .Should()
            .Be(expected);


    [Fact]
    public void EqualsNull() =>
        new Position(lat0, lng0)
            .Equals(null)
            .Should()
            .Be(false);


    [Theory]
    [InlineData(lat0, lng0, lat0, lng0, true)]
    [InlineData(lat0, lng0, lat1, lng0, false)]
    [InlineData(lat0, lng0, lat0, lng1, false)]
    public void GetHashCodeConsidersLatitudeAndLongitude(double lat1, double lng1, double lat2, double lng2, bool expected) =>
        new Position(lat1, lng1)
            .GetHashCode()
            .Equals(new Position(lat2, lng2).GetHashCode())
            .Should()
            .Be(expected);


    [Fact]
    public void ToStringReturnsLatitudeAndLongitude() =>
        new Position(lat0, lng0)
            .ToString()
            .Should()
            .Be($"Latitude: {lat0} - Longitude: {lng0}");
}
