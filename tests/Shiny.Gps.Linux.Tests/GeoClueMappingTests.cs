using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Shiny.Locations;
using Shiny.Locations.GeoClue;

namespace Shiny.Gps.Linux.Tests;


public class GeoClueMappingTests
{
    static readonly DateTimeOffset Now = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);


    [Fact]
    public void Precise_Request_Asks_For_Exact()
        => Assert.Equal(GeoClueAccuracyLevel.Exact, GeoClueMapping.AccuracyFor(new GpsRequest(RequestPreciseAccuracy: true), new LinuxGpsOptions()));


    [Fact]
    public void Normal_Request_Asks_For_Street()
        => Assert.Equal(GeoClueAccuracyLevel.Street, GeoClueMapping.AccuracyFor(GpsRequest.Foreground, new LinuxGpsOptions()));


    [Theory]
    [InlineData(GpsBackgroundMode.None)]
    [InlineData(GpsBackgroundMode.Standard)]
    [InlineData(GpsBackgroundMode.Realtime)]
    public void Background_Mode_Does_Not_Change_The_Level(GpsBackgroundMode mode)
        => Assert.Equal(GeoClueAccuracyLevel.Street, GeoClueMapping.AccuracyFor(new GpsRequest(mode), new LinuxGpsOptions()));


    [Fact]
    public void Options_Override_The_Levels()
    {
        var options = new LinuxGpsOptions { AccuracyLevel = GeoClueAccuracyLevel.City, PreciseAccuracyLevel = GeoClueAccuracyLevel.Street };
        Assert.Equal(GeoClueAccuracyLevel.City, GeoClueMapping.AccuracyFor(GpsRequest.Foreground, options));
        Assert.Equal(GeoClueAccuracyLevel.Street, GeoClueMapping.AccuracyFor(new GpsRequest(RequestPreciseAccuracy: true), options));
    }


    [Theory]
    [InlineData("org.freedesktop.DBus.Error.AccessDenied", AccessState.Denied)]
    [InlineData("org.freedesktop.DBus.Error.ServiceUnknown", AccessState.NotSupported)]
    [InlineData("org.freedesktop.DBus.Error.NameHasNoOwner", AccessState.NotSupported)]
    public void Errors_That_Decide_Access(string error, AccessState expected)
        => Assert.Equal(expected, GeoClueMapping.AccessFromError(error));


    [Theory]
    [InlineData("org.freedesktop.DBus.Error.NoReply")]
    [InlineData("org.freedesktop.DBus.Error.Failed")]
    [InlineData(null)]
    public void Errors_That_Decide_Nothing(string? error)
        => Assert.Null(GeoClueMapping.AccessFromError(error));


    [Fact]
    public void Reading_Maps_Known_Values()
    {
        var reading = GeoClueMapping.ToReading(43.65, -79.38, 25, 120, 3.5, 90, 1_764_072_000, 500_000, Now);

        Assert.Equal(43.65, reading.Position.Latitude);
        Assert.Equal(-79.38, reading.Position.Longitude);
        Assert.Equal(25, reading.PositionAccuracy);
        Assert.Equal(120, reading.Altitude);
        Assert.Equal(3.5, reading.Speed);
        Assert.Equal(90, reading.Heading);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_764_072_000).AddMilliseconds(500), reading.Timestamp);
    }


    [Fact]
    public void Reading_Maps_GeoClue_Unknowns()
    {
        var reading = GeoClueMapping.ToReading(1, 2, 3, -double.MaxValue, -1, -1, 0, 0, Now);

        Assert.Equal(0, reading.Altitude);
        Assert.Equal(-1, reading.Speed);
        Assert.Equal(-1, reading.Heading);
        Assert.Equal(Now, reading.Timestamp);
    }


    [Fact]
    public void Registration_Is_A_NoOp_Off_Linux()
    {
        var services = new ServiceCollection();
        services.AddLinuxGps();

        if (OperatingSystem.IsLinux())
            Assert.Contains(services, d => d.ServiceType == typeof(IGpsManager));
        else
            Assert.Empty(services);
    }
}
