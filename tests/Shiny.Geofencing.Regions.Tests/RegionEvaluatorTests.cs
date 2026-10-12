using Shiny.Extensions.Stores;
using Shiny.Locations;
using Xunit;

namespace Shiny.Geofencing.Regions.Tests;

/// <summary>
/// The evaluator against hand-rolled sources - no database, just the <see cref="IRegionSource"/> contract.
/// </summary>
public class RegionEvaluatorTests
{
    static readonly Position Here = new(39.75, -104.99);

    readonly MemoryKeyValueStore state = new();
    readonly RecordingDelegate handler = new();


    RegionEvaluator Evaluator(RegionGeofenceConfig config) => Geo.Evaluator(null, this.state, this.handler, config);


    [Fact]
    public async Task A_Failing_Source_Does_Not_Stop_Other_Sets()
    {
        var config = new RegionGeofenceConfig()
            .AddRegionSet("broken", new ThrowingSource())
            .AddRegionSet("working", new FixedSource("a"));

        var changes = await this.Evaluator(config).Evaluate(Here);

        var change = Assert.Single(changes);
        Assert.Equal("working", change.RegionSet);
        Assert.Equal("a", change.RegionId);
    }


    [Fact]
    public async Task A_Set_Named_Started_Does_Not_Collide_With_The_Manager_State()
    {
        // the manager persists "regiongeofence.started" - set ids live under their own prefix
        var config = new RegionGeofenceConfig().AddRegionSet("started", new FixedSource("a"));

        await this.Evaluator(config).Evaluate(Here);

        Assert.False(this.state.Contains("regiongeofence.started"));
    }


    [Fact]
    public async Task Sources_Are_Resolved_From_The_Container()
    {
        var resolved = 0;
        var config = new RegionGeofenceConfig().AddRegionSet("lazy", _ =>
        {
            resolved++;
            return new FixedSource("a");
        });

        var evaluator = this.Evaluator(config);
        await evaluator.Evaluate(Here);
        await evaluator.Evaluate(Here);

        Assert.Equal(1, resolved);
    }


    class FixedSource(string id) : IRegionSource
    {
        public Task<RegionMatch?> FindRegion(Position position, CancellationToken cancellationToken = default)
            => Task.FromResult<RegionMatch?>(new RegionMatch(id, id, id));

        public Task<RegionMatch?> GetRegion(string regionId, CancellationToken cancellationToken = default)
            => Task.FromResult<RegionMatch?>(new RegionMatch(regionId, regionId, regionId));
    }


    class ThrowingSource : IRegionSource
    {
        public Task<RegionMatch?> FindRegion(Position position, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("query failed");

        public Task<RegionMatch?> GetRegion(string regionId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("query failed");
    }
}
