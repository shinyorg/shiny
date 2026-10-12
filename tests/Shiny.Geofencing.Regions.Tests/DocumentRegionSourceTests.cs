using Shiny.DocumentDb;
using Shiny.Extensions.Stores;
using Shiny.Locations;
using Xunit;

namespace Shiny.Geofencing.Regions.Tests;

public class DocumentRegionSourceTests : IDisposable
{
    // Two adjacent boxes and a shop, all around Denver.
    static readonly Position Downtown = new(39.75, -104.99);
    static readonly Position Suburb = new(39.55, -104.99);
    static readonly Position Kansas = new(38.50, -98.00);

    readonly SqliteFixture db = new(opts =>
    {
        opts.ConfigureDocument<Zone>(cfg => cfg.MapSpatialProperty(nameof(Zone.Boundary), z => z.Boundary));
        opts.ConfigureDocument<Shop>(cfg => cfg.MapSpatialProperty(nameof(Shop.Location), s => s.Location));
    });
    readonly MemoryKeyValueStore state = new();
    readonly RecordingDelegate handler = new();


    DocumentStore Store => this.db.Store;


    public void Dispose() => this.db.Dispose();


    async Task SeedZones(CancellationToken ct)
    {
        await this.Store.BatchUpsert(new[]
        {
            new Zone { Id = "north", Name = "North Denver", Boundary = Geo.Box(39.70, -105.10, 39.90, -104.90) },
            new Zone { Id = "south", Name = "South Denver", Boundary = Geo.Box(39.50, -105.10, 39.70, -104.90) },
            new Zone { Id = "retired", Name = "Retired Zone", Active = false, Boundary = Geo.Box(39.70, -105.10, 39.90, -104.90) }
        }, cancellationToken: ct);
    }


    static RegionGeofenceConfig ZoneConfig() => new RegionGeofenceConfig()
        .AddDocumentRegionSet<Zone>("zones", z => z.Id, z => z.Name, filter: z => z.Active);


    RegionEvaluator Evaluator(RegionGeofenceConfig config)
        => Geo.Evaluator(this.Store, this.state, this.handler, config);


    [Fact]
    public async Task Entering_A_Zone_Raises_An_Entry()
    {
        var ct = CancellationToken.None;
        await this.SeedZones(ct);
        var evaluator = this.Evaluator(ZoneConfig());

        var changes = await evaluator.Evaluate(Downtown, ct);

        var change = Assert.Single(changes);
        Assert.True(change.Entered);
        Assert.Equal("zones", change.RegionSet);
        Assert.Equal("north", change.RegionId);
        Assert.Equal("North Denver", change.RegionName);
        Assert.Equal("North Denver", change.RegionAs<Zone>()!.Name);
        Assert.Equal(Downtown, change.Position);
        Assert.Equal(changes, this.handler.Changes);
    }


    [Fact]
    public async Task Staying_In_A_Zone_Raises_Nothing()
    {
        var ct = CancellationToken.None;
        await this.SeedZones(ct);
        var evaluator = this.Evaluator(ZoneConfig());

        await evaluator.Evaluate(Downtown, ct);
        var changes = await evaluator.Evaluate(new Position(39.80, -105.00), ct); // still inside "north"

        Assert.Empty(changes);
        Assert.Single(this.handler.Changes);
    }


    [Fact]
    public async Task Crossing_Into_A_Neighbour_Raises_Exit_Then_Entry()
    {
        var ct = CancellationToken.None;
        await this.SeedZones(ct);
        var evaluator = this.Evaluator(ZoneConfig());

        await evaluator.Evaluate(Downtown, ct);
        var changes = await evaluator.Evaluate(Suburb, ct);

        Assert.Equal(2, changes.Count);
        Assert.False(changes[0].Entered);
        Assert.Equal("north", changes[0].RegionId);
        Assert.Equal("North Denver", changes[0].RegionAs<Zone>()!.Name); // the exited region is still described
        Assert.True(changes[1].Entered);
        Assert.Equal("south", changes[1].RegionId);
    }


    [Fact]
    public async Task Leaving_Every_Zone_Raises_Only_An_Exit()
    {
        var ct = CancellationToken.None;
        await this.SeedZones(ct);
        var evaluator = this.Evaluator(ZoneConfig());

        await evaluator.Evaluate(Downtown, ct);
        var changes = await evaluator.Evaluate(Kansas, ct);

        var change = Assert.Single(changes);
        Assert.False(change.Entered);
        Assert.Equal("north", change.RegionId);
    }


    [Fact]
    public async Task Filtered_Documents_Are_Not_Monitored()
    {
        var ct = CancellationToken.None;
        await this.SeedZones(ct);
        var evaluator = this.Evaluator(ZoneConfig());

        // "retired" covers the exact same box as "north" but is filtered out, so it can never match.
        await evaluator.Evaluate(Downtown, ct);

        Assert.All(this.handler.Changes, c => Assert.NotEqual("retired", c.RegionId));
    }


    [Fact]
    public async Task Point_Regions_Match_In_Proximity_Mode()
    {
        var ct = CancellationToken.None;
        await this.Store.BatchUpsert(new[]
        {
            new Shop { Id = "downtown", Name = "Downtown Shop", Location = new GeoPoint(39.7505, -104.9905) },
            new Shop { Id = "airport", Name = "Airport Shop", Location = new GeoPoint(39.8561, -104.6737) }
        }, cancellationToken: ct);

        var config = new RegionGeofenceConfig()
            .AddDocumentRegionSet<Shop>("shops", s => s.Id, s => s.Name, withinMeters: 1000);
        var evaluator = this.Evaluator(config);

        var entered = await evaluator.Evaluate(Downtown, ct);
        var change = Assert.Single(entered);
        Assert.True(change.Entered);
        Assert.Equal("downtown", change.RegionId);

        // 30km away - out of range of both shops.
        var exited = await evaluator.Evaluate(new Position(39.50, -104.99), ct);
        Assert.False(Assert.Single(exited).Entered);
    }


    [Fact]
    public async Task Nearest_Region_Wins_In_Proximity_Mode()
    {
        var ct = CancellationToken.None;
        await this.Store.BatchUpsert(new[]
        {
            new Shop { Id = "near", Name = "Near", Location = new GeoPoint(39.7501, -104.9901) },
            new Shop { Id = "far", Name = "Far", Location = new GeoPoint(39.7600, -104.9900) }
        }, cancellationToken: ct);

        var config = new RegionGeofenceConfig()
            .AddDocumentRegionSet<Shop>("shops", s => s.Id, withinMeters: 5000);

        var changes = await this.Evaluator(config).Evaluate(Downtown, ct);
        Assert.Equal("near", Assert.Single(changes).RegionId);
    }


    [Fact]
    public async Task Sets_Are_Tracked_Independently()
    {
        var ct = CancellationToken.None;
        await this.SeedZones(ct);
        await this.Store.Upsert(new Shop { Id = "downtown", Name = "Downtown Shop", Location = new GeoPoint(39.7505, -104.9905) }, cancellationToken: ct);

        var config = new RegionGeofenceConfig()
            .AddDocumentRegionSet<Zone>("zones", z => z.Id, z => z.Name, filter: z => z.Active)
            .AddDocumentRegionSet<Shop>("shops", s => s.Id, s => s.Name, withinMeters: 1000);
        var evaluator = this.Evaluator(config);

        var changes = await evaluator.Evaluate(Downtown, ct);
        Assert.Equal(2, changes.Count);
        Assert.Contains(changes, c => c.RegionSet == "zones" && c.RegionId == "north");
        Assert.Contains(changes, c => c.RegionSet == "shops" && c.RegionId == "downtown");

        // Moving south leaves the shop behind but stays in the zone layer.
        var moved = await evaluator.Evaluate(Suburb, ct);
        Assert.Equal(3, moved.Count);
        Assert.Contains(moved, c => c.RegionSet == "shops" && !c.Entered);
        Assert.Contains(moved, c => c.RegionSet == "zones" && c.Entered && c.RegionId == "south");
    }


    [Fact]
    public async Task A_Restart_Does_Not_Replay_The_Entry()
    {
        var ct = CancellationToken.None;
        await this.SeedZones(ct);

        await this.Evaluator(ZoneConfig()).Evaluate(Downtown, ct);
        Assert.Single(this.handler.Changes);

        // New evaluator, same persisted state - this is what a relaunched process sees.
        var restarted = this.Evaluator(ZoneConfig());
        var changes = await restarted.Evaluate(Downtown, ct);

        Assert.Empty(changes);
        Assert.Single(this.handler.Changes);
    }


    [Fact]
    public async Task An_Exit_After_A_Restart_Still_Describes_The_Region()
    {
        var ct = CancellationToken.None;
        await this.SeedZones(ct);

        await this.Evaluator(ZoneConfig()).Evaluate(Downtown, ct);

        // The in-memory cache died with the old evaluator, so the region is re-read by id.
        var changes = await this.Evaluator(ZoneConfig()).Evaluate(Kansas, ct);

        var exit = Assert.Single(changes);
        Assert.False(exit.Entered);
        Assert.Equal("north", exit.RegionId);
        Assert.Equal("North Denver", exit.RegionAs<Zone>()!.Name);
    }


    [Fact]
    public async Task An_Exit_From_A_Deleted_Region_Still_Raises()
    {
        var ct = CancellationToken.None;
        await this.SeedZones(ct);

        await this.Evaluator(ZoneConfig()).Evaluate(Downtown, ct);
        await this.Store.Remove<Zone>("north", ct);

        var changes = await this.Evaluator(ZoneConfig()).Evaluate(Kansas, ct);

        var exit = Assert.Single(changes);
        Assert.False(exit.Entered);
        Assert.Equal("north", exit.RegionId);
        Assert.Null(exit.Region);
    }


    [Fact]
    public async Task A_Throwing_Delegate_Does_Not_Stop_Later_Transitions()
    {
        var ct = CancellationToken.None;
        await this.SeedZones(ct);
        var evaluator = this.Evaluator(ZoneConfig());
        this.handler.Throw = true;

        await evaluator.Evaluate(Downtown, ct);
        await evaluator.Evaluate(Suburb, ct);

        // entry(north), exit(north), entry(south) - all delivered despite every one of them throwing.
        Assert.Equal(3, this.handler.Changes.Count);
        Assert.Equal("south", this.handler.Changes[^1].RegionId);
    }


    [Fact]
    public async Task Current_Reports_Without_Raising_Or_Recording()
    {
        var ct = CancellationToken.None;
        await this.SeedZones(ct);
        var evaluator = this.Evaluator(ZoneConfig());

        var current = await evaluator.Current(Downtown, ct);
        var zone = Assert.Single(current);
        Assert.Equal("north", zone.RegionId);
        Assert.Equal("North Denver", zone.RegionAs<Zone>()!.Name);
        Assert.Empty(this.handler.Changes);

        // Nothing was recorded, so the first real reading still counts as an entry.
        var changes = await evaluator.Evaluate(Downtown, ct);
        Assert.True(Assert.Single(changes).Entered);
    }


    [Fact]
    public async Task Current_Reports_Null_Outside_Every_Region()
    {
        var ct = CancellationToken.None;
        await this.SeedZones(ct);

        var current = await this.Evaluator(ZoneConfig()).Current(Kansas, ct);

        var zone = Assert.Single(current);
        Assert.Null(zone.RegionId);
        Assert.Null(zone.Region);
    }


    [Fact]
    public async Task Reset_Forgets_The_Current_Region()
    {
        var ct = CancellationToken.None;
        await this.SeedZones(ct);
        var evaluator = this.Evaluator(ZoneConfig());

        await evaluator.Evaluate(Downtown, ct);
        evaluator.Reset();
        var changes = await evaluator.Evaluate(Downtown, ct);

        Assert.True(Assert.Single(changes).Entered);
    }
}
