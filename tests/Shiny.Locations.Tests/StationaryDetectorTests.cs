using System;
using Shiny.Locations;
using Xunit;

namespace Shiny.Locations.Tests;


public class StationaryDetectorTests
{
    static GpsReading R(double lat, double lon, DateTimeOffset at)
        => new(new Position(lat, lon), 1, at, 0, 0, 0, 0, 0);


    static Position Offset(Position origin, double meters)
    {
        // ~111,000 meters per degree of latitude
        return new Position(origin.Latitude + meters / 111_000.0, origin.Longitude);
    }


    [Fact]
    public void FirstReading_NeverStationary()
    {
        var det = new StationaryDetector();
        var stationary = det.Detect(R(0, 0, DateTimeOffset.UtcNow), 50, 30);
        Assert.False(stationary);
        Assert.False(det.IsStationary);
    }


    [Fact]
    public void StaysWithinThreshold_ForLongEnough_BecomesStationary()
    {
        var det = new StationaryDetector();
        var t0 = DateTimeOffset.UtcNow;
        det.Detect(R(0, 0, t0), metersThreshold: 100, secondsThreshold: 30);
        det.Detect(R(0.0001, 0, t0.AddSeconds(5)), 100, 30);       // ~11m later

        var stationary = det.Detect(R(0.0001, 0, t0.AddSeconds(60)), 100, 30); // 60s later, still ~11m
        Assert.True(stationary);
        Assert.True(det.IsStationary);
    }


    [Fact]
    public void MovementBeyondThreshold_BreaksStationary_AndResetsMovementClock()
    {
        var det = new StationaryDetector();
        var t0 = DateTimeOffset.UtcNow;
        det.Detect(R(0, 0, t0), 50, 30);
        det.Detect(R(0, 0, t0.AddSeconds(60)), 50, 30);
        Assert.True(det.IsStationary);

        // jump 200m away — beyond threshold
        var moved = det.Detect(R(0.002, 0, t0.AddSeconds(70)), 50, 30); // ~222m
        Assert.False(moved);
        Assert.False(det.IsStationary);
    }


    [Fact]
    public void Reset_ClearsState()
    {
        var det = new StationaryDetector();
        var t0 = DateTimeOffset.UtcNow;
        det.Detect(R(0, 0, t0), 50, 30);
        det.Detect(R(0, 0, t0.AddSeconds(60)), 50, 30);
        Assert.True(det.IsStationary);

        det.Reset();
        Assert.False(det.IsStationary);

        // First reading after reset is "first" again
        var afterReset = det.Detect(R(0, 0, t0.AddSeconds(120)), 50, 30);
        Assert.False(afterReset);
    }


    [Fact]
    public void Detect_ReturnValue_MatchesIsStationary()
    {
        var det = new StationaryDetector();
        var t0 = DateTimeOffset.UtcNow;
        var r1 = det.Detect(R(0, 0, t0), 50, 30);
        Assert.Equal(det.IsStationary, r1);

        var r2 = det.Detect(R(0, 0, t0.AddSeconds(60)), 50, 30);
        Assert.Equal(det.IsStationary, r2);
    }
}
