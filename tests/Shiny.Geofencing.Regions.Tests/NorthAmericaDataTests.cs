using Microsoft.Extensions.DependencyInjection;
using Shiny.DocumentDb;
using Shiny.Locations;
using Shiny.Locations.NorthAmerica;
using Xunit;

namespace Shiny.Geofencing.Regions.Tests;

public class NorthAmericaDataTests : IDisposable
{
    readonly SqliteFixture db = new(opts => opts.MapNorthAmericaData());


    DocumentStore Store => this.db.Store;


    public void Dispose() => this.db.Dispose();


    [Fact]
    public void DataSets_Have_Expected_Counts()
    {
        // 50 states + DC = 51 US regions; 13 Canadian provinces/territories.
        Assert.Equal(51, NorthAmericaData.Regions.Count(r => r.CountryCode == NorthAmericaData.UnitedStates));
        Assert.Equal(13, NorthAmericaData.Regions.Count(r => r.CountryCode == NorthAmericaData.Canada));
        Assert.All(NorthAmericaData.Cities, c => Assert.False(string.IsNullOrWhiteSpace(c.Name)));
        Assert.NotEmpty(NorthAmericaData.Cities);
    }

    [Fact]
    public void Ids_Are_Unique()
    {
        var regionIds = NorthAmericaData.Regions.Select(r => r.Id).ToArray();
        Assert.Equal(regionIds.Length, regionIds.Distinct().Count());

        var cityIds = NorthAmericaData.Cities.Select(c => c.Id).ToArray();
        Assert.Equal(cityIds.Length, cityIds.Distinct().Count());
    }

    [Fact]
    public void Boundaries_Are_Closed_Rings()
    {
        foreach (var region in NorthAmericaData.Regions)
        {
            var ring = Assert.IsType<GeoPolygon>(region.Boundary).ExteriorRing;
            Assert.True(ring.Count >= 4, $"{region.Id} ring too short");
            Assert.Equal(ring[0], ring[^1]); // closed
        }
    }

    [Fact]
    public void City_Location_Preserves_Lat_Lon_Axis_Order()
    {
        var denver = NorthAmericaData.Cities.Single(c => c.Id == "US-CO-denver");
        Assert.Equal(39.7392, denver.Location.Latitude, 3);
        Assert.Equal(-104.9903, denver.Location.Longitude, 3);
    }

    [Fact]
    public async Task Seeder_Populates_Store()
    {
        var ct = CancellationToken.None;
        await new NorthAmericaSeeder().SeedAsync(this.Store, ct);

        Assert.Equal(NorthAmericaData.Regions.Count, await this.Store.Count<GeoRegion>(cancellationToken: ct));
        Assert.Equal(NorthAmericaData.Cities.Count, await this.Store.Count<GeoCity>(cancellationToken: ct));
    }

    [Fact]
    public async Task Seeder_Is_Idempotent()
    {
        var ct = CancellationToken.None;
        var seeder = new NorthAmericaSeeder();
        await seeder.SeedAsync(this.Store, ct);
        await seeder.SeedAsync(this.Store, ct);

        Assert.Equal(NorthAmericaData.Regions.Count, await this.Store.Count<GeoRegion>(cancellationToken: ct));
    }

    [Fact]
    public async Task Point_Falls_Inside_Its_Region()
    {
        var ct = CancellationToken.None;
        await new NorthAmericaSeeder().SeedAsync(this.Store, ct);

        // Denver sits well inside Colorado's rectangular boundary.
        Geometry denver = new GeoPoint(39.7392, -104.9903);
        var hits = await this.Store.GeoIntersects<GeoRegion>(denver, ct: ct);

        Assert.Contains(hits, h => h.Document.Id == "US-CO");
        Assert.DoesNotContain(hits, h => h.Document.Id == "US-WY"); // neighbouring state, not a match
    }

    [Fact]
    public async Task Nearby_Cities_Found_By_Distance()
    {
        var ct = CancellationToken.None;
        await new NorthAmericaSeeder().SeedAsync(this.Store, ct);

        Geometry origin = new GeoPoint(40.7128, -74.0060); // New York City
        var hits = await this.Store.GeoWithinDistance<GeoCity>(origin, 50_000, orderByDistanceFrom: origin, ct: ct);

        Assert.NotEmpty(hits);
        Assert.Equal("US-NY-new-york", hits[0].Document.Id); // closest to the origin
    }


    [Fact]
    public async Task AddNorthAmericaSeeder_Seeds_From_A_Shiny_Startup_Task()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IDocumentStore>(this.Store)
            .AddNorthAmericaSeeder()
            .BuildServiceProvider();

        // MAUI never runs IHostedService - the Shiny host runs startup tasks instead
        var task = Assert.Single(services.GetServices<Shiny.IShinyStartupTask>());
        task.Start();

        var deadline = DateTime.UtcNow.AddSeconds(10);
        var count = 0;
        while (count < NorthAmericaData.Regions.Count && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
            count = await this.Store.Count<GeoRegion>();
        }
        Assert.Equal(NorthAmericaData.Regions.Count, count);
    }
}
