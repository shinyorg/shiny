using Shiny.Extensions.Stores;
using Shiny.Locations;
using Shiny.Locations.NorthAmerica;
using Xunit;

namespace Shiny.Geofencing.Regions.Tests;

/// <summary>
/// The shipped US/Canada reference dataset used as a geofence source - state boundaries in containment mode
/// and city centres in proximity mode, together, off one store.
/// </summary>
public class NorthAmericaGeofenceTests : IAsyncLifetime
{
    static readonly Position Denver = new(39.7392, -104.9903);
    static readonly Position Casper = new(42.8666, -106.3131);   // Wyoming
    static readonly Position Toronto = new(43.6532, -79.3832);

    readonly SqliteFixture db = new(opts => opts.MapNorthAmericaData());
    readonly MemoryKeyValueStore state = new();
    readonly RecordingDelegate handler = new();


    public Task InitializeAsync() => new NorthAmericaSeeder().SeedAsync(this.db.Store, CancellationToken.None);


    public Task DisposeAsync()
    {
        this.db.Dispose();
        return Task.CompletedTask;
    }


    RegionEvaluator Evaluator() => Geo.Evaluator(
        this.db.Store,
        this.state,
        this.handler,
        new RegionGeofenceConfig()
            .AddNorthAmericanRegions()
            .AddNorthAmericanCities()
    );


    [Fact]
    public async Task Driving_Denver_To_Casper_Crosses_The_State_Line()
    {
        var ct = CancellationToken.None;
        var evaluator = this.Evaluator();

        var arrival = await evaluator.Evaluate(Denver, ct);
        Assert.Contains(arrival, c => c is { RegionSet: "regions", Entered: true, RegionId: "US-CO" });
        Assert.Contains(arrival, c => c is { RegionSet: "cities", Entered: true, RegionId: "US-CO-denver" });

        var departure = await evaluator.Evaluate(Casper, ct);
        Assert.Contains(departure, c => c is { RegionSet: "regions", Entered: false, RegionId: "US-CO" });
        Assert.Contains(departure, c => c is { RegionSet: "regions", Entered: true, RegionId: "US-WY" });
        Assert.Contains(departure, c => c is { RegionSet: "cities", Entered: false, RegionId: "US-CO-denver" });

        // The exit always precedes the entry for the same set.
        var regionChanges = departure.Where(c => c.RegionSet == "regions").ToList();
        Assert.False(regionChanges[0].Entered);
        Assert.True(regionChanges[1].Entered);
    }


    [Fact]
    public async Task Regions_Carry_Their_Document_Across_Countries()
    {
        var ct = CancellationToken.None;

        var changes = await this.Evaluator().Evaluate(Toronto, ct);

        var region = Assert.Single(changes, c => c.RegionSet == "regions");
        Assert.Equal("CA-ON", region.RegionId);
        Assert.Equal("Ontario", region.RegionAs<GeoRegion>()!.Name);
        Assert.Equal("CA", region.RegionAs<GeoRegion>()!.CountryCode);
    }


    [Fact]
    public async Task Cities_Can_Be_Filtered_By_Population()
    {
        var evaluator = Geo.Evaluator(
            this.db.Store,
            this.state,
            this.handler,
            new RegionGeofenceConfig().AddNorthAmericanCities(filter: c => c.Population >= 1_000_000_000)
        );

        var changes = await evaluator.Evaluate(Denver);

        Assert.Empty(changes);
    }
}
