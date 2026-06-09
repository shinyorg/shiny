using System;
using Shiny.Locations;
using Xunit;

namespace Shiny.Locations.Tests;


public class PositionTests
{
    // -------- Construction validation --------

    [Theory]
    [InlineData(-90.1, 0)]
    [InlineData(90.1, 0)]
    [InlineData(0, -180.1)]
    [InlineData(0, 180.1)]
    public void Construct_InvalidRanges_Throw(double lat, double lon)
    {
        Assert.Throws<ArgumentException>(() => new Position(lat, lon));
    }


    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    [InlineData(43.6532, -79.3832)]
    public void Construct_AtBoundaries_Succeeds(double lat, double lon)
    {
        var p = new Position(lat, lon);
        Assert.Equal(lat, p.Latitude);
        Assert.Equal(lon, p.Longitude);
    }


    // -------- Distance via instance method --------

    [Fact]
    public void GetDistanceTo_MatchesDistanceBetween()
    {
        var a = new Position(43.6532, -79.3832);
        var b = new Position(45.5017, -73.5673);

        Assert.Equal(
            Distance.Between(a, b).TotalKilometers,
            a.GetDistanceTo(b).TotalKilometers,
            6
        );
    }


    // -------- Bearing --------

    [Fact]
    public void Bearing_DueNorth_Is0()
    {
        var origin = new Position(0, 0);
        var north = new Position(1, 0);
        Assert.Equal(0, origin.GetCompassBearingTo(north), 1);
    }


    [Fact]
    public void Bearing_DueEast_Is90()
    {
        var origin = new Position(0, 0);
        var east = new Position(0, 1);
        Assert.Equal(90, origin.GetCompassBearingTo(east), 1);
    }


    [Fact]
    public void Bearing_DueSouth_Is180()
    {
        var origin = new Position(0, 0);
        var south = new Position(-1, 0);
        Assert.Equal(180, origin.GetCompassBearingTo(south), 1);
    }


    [Fact]
    public void Bearing_DueWest_Is270()
    {
        var origin = new Position(0, 0);
        var west = new Position(0, -1);
        Assert.Equal(270, origin.GetCompassBearingTo(west), 1);
    }


    [Fact]
    public void Bearing_AlwaysIn_0_to_360()
    {
        var origin = new Position(0, 0);
        var samples = new[]
        {
            new Position(45, 45), new Position(-45, -45),
            new Position(45, -45), new Position(-45, 45),
            new Position(0, 179.9), new Position(0, -179.9)
        };

        foreach (var s in samples)
        {
            var b = origin.GetCompassBearingTo(s);
            Assert.InRange(b, 0.0, 360.0);
        }
    }


    [Fact]
    public void Bearing_WrapsAtAntimeridian_NotMoreThan180Diff()
    {
        // Just east of antimeridian → just west of antimeridian: the bearing should still
        // be a small angle (~270° westward), not a 359° eastward turn.
        var east = new Position(0, 179);
        var west = new Position(0, -179);
        var bearing = east.GetCompassBearingTo(west);
        Assert.InRange(bearing, 85, 95);
    }


    // -------- Helpers --------

    [Theory]
    [InlineData(0, 0)]
    [InlineData(180, Math.PI)]
    [InlineData(90, Math.PI / 2)]
    public void ToRad_Conversions(double degrees, double expectedRadians)
        => Assert.Equal(expectedRadians, Position.ToRad(degrees), 9);


    [Theory]
    [InlineData(0, 0)]
    [InlineData(Math.PI, 180)]
    [InlineData(Math.PI / 2, 90)]
    public void ToDegrees_Conversions(double radians, double expectedDegrees)
        => Assert.Equal(expectedDegrees, Position.ToDegrees(radians), 9);


    [Theory]
    [InlineData(0, 0)]
    [InlineData(Math.PI / 2, 90)]
    [InlineData(-Math.PI / 2, 270)]
    [InlineData(Math.PI, 180)]
    public void ToBearing_NormalizesToCompassRange(double radians, double expectedDegrees)
        => Assert.Equal(expectedDegrees, Position.ToBearing(radians), 6);
}
