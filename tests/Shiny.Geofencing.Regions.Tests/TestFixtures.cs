using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Data.Sqlite;
using Shiny.DocumentDb;
using Shiny.DocumentDb.Sqlite;
using Shiny.Extensions.Stores;
using Shiny.Locations;

namespace Shiny.Geofencing.Regions.Tests;

/// <summary>A polygon region - the shape a state/city boundary or a delivery zone takes.</summary>
public class Zone
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool Active { get; set; } = true;
    public Geometry Boundary { get; set; } = null!;
}


/// <summary>A point region - only ever matchable in proximity mode.</summary>
public class Shop
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public GeoPoint Location { get; set; }
}


/// <summary>Collects every transition the evaluator raises, optionally throwing first.</summary>
public class RecordingDelegate : IRegionGeofenceDelegate
{
    public List<RegionChange> Changes { get; } = new();
    public bool Throw { get; set; }

    public Task OnRegionChanged(RegionChange change)
    {
        this.Changes.Add(change);
        if (this.Throw)
            throw new InvalidOperationException("delegate blew up");

        return Task.CompletedTask;
    }
}


/// <summary>An in-memory SQLite document store that lives as long as the fixture.</summary>
public sealed class SqliteFixture : IDisposable
{
    readonly SqliteConnection holdOpen;

    public SqliteFixture(Action<DocumentStoreOptions> configure)
    {
        var cs = $"Data Source=geofence_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        this.holdOpen = new SqliteConnection(cs);
        this.holdOpen.Open();

        var opts = new DocumentStoreOptions
        {
            DatabaseProvider = new SqliteDatabaseProvider(cs),
            TableName = $"t{Guid.NewGuid():N}"
        };
        configure(opts);
        this.Store = new DocumentStore(opts);
    }

    public DocumentStore Store { get; }

    public void Dispose()
    {
        this.Store.Dispose();
        this.holdOpen.Dispose();
    }
}


public static class Geo
{
    /// <summary>An axis-aligned box, as a closed ring.</summary>
    public static GeoPolygon Box(double minLat, double minLng, double maxLat, double maxLng) => new(new[]
    {
        new GeoPoint(minLat, minLng),
        new GeoPoint(minLat, maxLng),
        new GeoPoint(maxLat, maxLng),
        new GeoPoint(maxLat, minLng),
        new GeoPoint(minLat, minLng)
    });

    public static RegionEvaluator Evaluator(
        IDocumentStore? store,
        IKeyValueStore state,
        IRegionGeofenceDelegate del,
        RegionGeofenceConfig config
    )
    {
        var services = new ServiceCollection();
        if (store != null)
            services.AddSingleton(store);

        return new(services.BuildServiceProvider(), state, del, config, NullLogger<RegionEvaluator>.Instance);
    }
}
