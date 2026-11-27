using Shiny.DocumentDb;
using Shiny.Locations;
using Shiny.Locations.NorthAmerica;
using Xunit;

namespace Shiny.Geofencing.Regions.Tests;

/// <summary>
/// With reflection fallback off, the store can only use the metadata MapNorthAmericaData registers - the same position
/// a trimmed / NativeAOT app is in, where the reflection resolver saw the seed marker's Id without its getter.
/// </summary>
public class NorthAmericaTrimSafetyTests : IDisposable
{
    readonly SqliteFixture db = new(opts =>
    {
        opts.UseReflectionFallback = false;
        opts.MapNorthAmericaData();
    });


    public void Dispose() => this.db.Dispose();


    [Fact]
    public async Task Seeds_And_Records_The_Marker_Without_Reflection()
    {
        var ct = CancellationToken.None;

        var applied = await DocumentSeedRunner.RunAsync(this.db.Store, [new NorthAmericaSeeder()], cancellationToken: ct);
        Assert.Equal([NorthAmericaSeeder.SeederName], applied);

        var marker = await this.db.Store.Get<DocumentSeedMarker>(NorthAmericaSeeder.SeederName, cancellationToken: ct);
        Assert.NotNull(marker);
        Assert.Equal(new NorthAmericaSeeder().Version, marker.Version);

        // run-once: the recorded marker stops a second run
        var again = await DocumentSeedRunner.RunAsync(this.db.Store, [new NorthAmericaSeeder()], cancellationToken: ct);
        Assert.Empty(again);
    }


    [Fact]
    public async Task Regions_And_Cities_Round_Trip_Without_Reflection()
    {
        var ct = CancellationToken.None;
        await new NorthAmericaSeeder().SeedAsync(this.db.Store, ct);

        var colorado = await this.db.Store.Get<GeoRegion>("US-CO", cancellationToken: ct);
        Assert.NotNull(colorado);
        Assert.Equal("Colorado", colorado.Name);
        Assert.IsType<GeoPolygon>(colorado.Boundary);

        var denver = await this.db.Store.Get<GeoCity>("US-CO-denver", cancellationToken: ct);
        Assert.NotNull(denver);
        Assert.Equal("CO", denver.RegionCode);
        Assert.NotEqual(default, denver.Location);

        var within = await this.db.Store.GeoIntersects<GeoRegion>(new GeoPoint(39.7392, -104.9903));
        Assert.Contains(within, r => r.Document.Id == "US-CO");
    }
}
