using Shiny.Locations;
using Xunit;

namespace Shiny.Geofencing.Regions.Tests;

public class RegionGeofenceConfigTests
{
    [Fact]
    public void Defaults_Throttle_Readings_And_Track_In_The_Background()
    {
        var config = new RegionGeofenceConfig();

        Assert.Equal(300, config.MinimumDistance!.TotalMeters);
        Assert.Equal(TimeSpan.FromMinutes(1), config.MinimumTime);
        Assert.Null(config.MaximumDistance);
        Assert.Null(config.MaximumTime);
        Assert.Equal(GpsBackgroundMode.Realtime, config.GpsRequest.BackgroundMode);
        Assert.Empty(config.RegionSets);
    }


    [Fact]
    public void Containment_Is_The_Default_Mode()
    {
        using var db = new SqliteFixture(_ => { });
        var source = new DocumentRegionSource<Zone>(db.Store, z => z.Id);

        Assert.Null(source.WithinMeters);
    }


    [Fact]
    public void Duplicate_Set_Names_Are_Rejected()
    {
        var config = new RegionGeofenceConfig().AddDocumentRegionSet<Zone>("zones", z => z.Id);

        Assert.Throws<ArgumentException>(() => config.AddDocumentRegionSet<Shop>("ZONES", s => s.Id));
    }


    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void A_Non_Positive_Radius_Is_Rejected(double meters)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new RegionGeofenceConfig().AddDocumentRegionSet<Shop>("shops", s => s.Id, withinMeters: meters)
        );


    [Fact]
    public void A_Blank_Set_Name_Is_Rejected()
        => Assert.Throws<ArgumentException>(() => new RegionGeofenceConfig().AddDocumentRegionSet<Zone>(" ", z => z.Id));
}
