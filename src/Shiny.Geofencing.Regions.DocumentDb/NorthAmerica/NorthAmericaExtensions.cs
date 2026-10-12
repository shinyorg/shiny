using System;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Shiny.DocumentDb;
using Shiny.Locations.NorthAmerica;

namespace Shiny.Locations;


/// <summary>
/// Registration helpers for the North American reference regions.
/// </summary>
public static class NorthAmericaExtensions
{
    /// <summary>
    /// Registers the <see cref="NorthAmericaSeeder"/> so the embedded US/Canada regions and cities are
    /// written into the target store once, when the Shiny host starts (it runs as an <see cref="IShinyStartupTask"/>,
    /// so it works in MAUI, where document seeders registered with <c>AddDocumentSeeder</c> never run). Until the
    /// first seed finishes, region and city sets match nothing.
    /// <para>
    /// Call <see cref="MapNorthAmericaData"/> in your <c>AddDocumentStore</c> options so spatial queries work
    /// against <see cref="GeoRegion.Boundary"/> / <see cref="GeoCity.Location"/>.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="storeName">The keyed store to seed, or null for the default store.</param>
    public static IServiceCollection AddNorthAmericaSeeder(this IServiceCollection services, string? storeName = null)
        => services.AddSingleton<IShinyStartupTask>(sp => new NorthAmericaSeedTask(
            sp,
            storeName,
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<NorthAmericaSeedTask>>()
        ));


    /// <summary>
    /// Maps the spatial properties of <see cref="GeoRegion"/> and <see cref="GeoCity"/> so containment /
    /// proximity queries work.
    /// </summary>
    /// <param name="options">The document store options.</param>
    public static DocumentStoreOptions MapNorthAmericaData(this DocumentStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.ConfigureDocument<GeoRegion>(cfg => cfg.MapSpatialProperty(nameof(GeoRegion.Boundary), r => r.Boundary));
        options.ConfigureDocument<GeoCity>(cfg => cfg.MapSpatialProperty(nameof(GeoCity.Location), c => c.Location));
        return options;
    }


    /// <summary>
    /// Monitors US states and Canadian provinces (containment) as a geofence region set.
    /// </summary>
    /// <param name="config">The region geofence config.</param>
    /// <param name="name">The region set name. Defaults to <c>regions</c>.</param>
    /// <param name="filter">Optional predicate narrowing which regions are monitored (e.g. one country).</param>
    /// <param name="storeName">The keyed document store holding the seeded data, or null for the default store.</param>
    public static RegionGeofenceConfig AddNorthAmericanRegions(
        this RegionGeofenceConfig config,
        string name = "regions",
        Expression<Func<GeoRegion, bool>>? filter = null,
        string? storeName = null
    ) => config.AddDocumentRegionSet<GeoRegion>(
        name,
        x => x.Id,
        x => x.Name,
        filter: filter,
        storeName: storeName
    );


    /// <summary>
    /// Monitors US and Canadian cities (proximity to the city centre) as a geofence region set.
    /// </summary>
    /// <param name="config">The region geofence config.</param>
    /// <param name="withinMeters">How close to a city centre counts as "in" the city. Defaults to 25km.</param>
    /// <param name="name">The region set name. Defaults to <c>cities</c>.</param>
    /// <param name="filter">Optional predicate narrowing which cities are monitored (e.g. a minimum population).</param>
    /// <param name="storeName">The keyed document store holding the seeded data, or null for the default store.</param>
    public static RegionGeofenceConfig AddNorthAmericanCities(
        this RegionGeofenceConfig config,
        double withinMeters = 25_000,
        string name = "cities",
        Expression<Func<GeoCity, bool>>? filter = null,
        string? storeName = null
    ) => config.AddDocumentRegionSet<GeoCity>(
        name,
        x => x.Id,
        x => x.Name,
        withinMeters,
        filter,
        storeName
    );
}
