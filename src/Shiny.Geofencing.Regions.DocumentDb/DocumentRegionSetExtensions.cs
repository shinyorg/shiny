using System;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Shiny.DocumentDb;

namespace Shiny.Locations;


/// <summary>
/// Registers Shiny.DocumentDb document types as geofence region sets.
/// </summary>
public static class DocumentRegionSetExtensions
{
    /// <summary>
    /// Registers a document type as a layer of geofence regions. The type must have a geometry mapped with
    /// <c>MapSpatialProperty</c> in a SQLite document store registered through <c>AddDocumentStore</c>.
    /// </summary>
    /// <typeparam name="T">The region document type.</typeparam>
    /// <param name="config">The region geofence config.</param>
    /// <param name="name">A stable name for the set - echoed on every change and used as the persistence key.</param>
    /// <param name="idSelector">
    /// Returns the document's id. This must be the value the store keys the document by, since it is used
    /// to re-read a region when describing an exit after an app restart.
    /// </param>
    /// <param name="nameSelector">Optional display name pulled onto each change event.</param>
    /// <param name="withinMeters">
    /// Null (default) for containment. Set it to switch to proximity, which point regions need.
    /// </param>
    /// <param name="filter">Optional predicate narrowing which documents of this type are monitored.</param>
    /// <param name="storeName">The keyed document store to query, or null for the default (unkeyed) store.</param>
    public static RegionGeofenceConfig AddDocumentRegionSet<T>(
        this RegionGeofenceConfig config,
        string name,
        Func<T, string> idSelector,
        Func<T, string?>? nameSelector = null,
        double? withinMeters = null,
        Expression<Func<T, bool>>? filter = null,
        string? storeName = null
    ) where T : class
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(idSelector);

        // validated up front so a bad radius fails at registration, not when the first reading arrives
        if (withinMeters is <= 0)
            throw new ArgumentOutOfRangeException(nameof(withinMeters), withinMeters, "Must be greater than zero.");

        return config.AddRegionSet(name, sp =>
        {
            var store = storeName == null
                ? sp.GetRequiredService<IDocumentStore>()
                : sp.GetRequiredKeyedService<IDocumentStore>(storeName);

            return new DocumentRegionSource<T>(store, idSelector, nameSelector, withinMeters, filter);
        });
    }
}
