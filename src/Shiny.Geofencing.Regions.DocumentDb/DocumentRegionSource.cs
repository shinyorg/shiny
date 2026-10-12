using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Shiny.DocumentDb;

namespace Shiny.Locations;


/// <summary>
/// An <see cref="IRegionSource"/> over one document type in a Shiny.DocumentDb SQLite store. The
/// type needs its geometry mapped with <c>MapSpatialProperty</c> in the store's options.
/// </summary>
/// <typeparam name="T">The region document type.</typeparam>
public class DocumentRegionSource<T> : IRegionSource where T : class
{
    readonly IDocumentStore store;
    readonly Func<T, string> idSelector;
    readonly Func<T, string?>? nameSelector;
    readonly double? withinMeters;
    readonly Expression<Func<T, bool>>? filter;


    /// <summary>Creates the source.</summary>
    /// <param name="store">The document store holding the regions. Its provider must support spatial queries (SQLite does).</param>
    /// <param name="idSelector">
    /// Returns the document's id. This must be the value the store keys the document by, since it is used
    /// to re-read a region when describing an exit after an app restart.
    /// </param>
    /// <param name="nameSelector">Optional display name pulled onto each change event.</param>
    /// <param name="withinMeters">
    /// Null (default) for containment - the device is inside the region when the GPS point falls inside the
    /// stored geometry. Set it to switch to proximity, which point regions (a city centre, a store location)
    /// need since a GPS point never falls "inside" another point.
    /// </param>
    /// <param name="filter">Optional predicate narrowing which documents of this type are monitored.</param>
    /// <exception cref="NotSupportedException">The store's provider does not support spatial queries.</exception>
    public DocumentRegionSource(
        IDocumentStore store,
        Func<T, string> idSelector,
        Func<T, string?>? nameSelector = null,
        double? withinMeters = null,
        Expression<Func<T, bool>>? filter = null
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(idSelector);

        if (withinMeters is <= 0)
            throw new ArgumentOutOfRangeException(nameof(withinMeters), withinMeters, "Must be greater than zero.");

        if (!store.SupportsSpatial)
            throw new NotSupportedException("The document store's provider does not support spatial queries, so it cannot back geofencing. Use Shiny.DocumentDb.Sqlite.");

        this.store = store;
        this.idSelector = idSelector;
        this.nameSelector = nameSelector;
        this.withinMeters = withinMeters;
        this.filter = filter;
    }


    /// <summary>
    /// The proximity radius in meters, or null when the source matches by containment.
    /// </summary>
    public double? WithinMeters => this.withinMeters;


    /// <inheritdoc />
    public async Task<RegionMatch?> FindRegion(Position position, CancellationToken cancellationToken = default)
    {
        Geometry point = new GeoPoint(position.Latitude, position.Longitude);

        // orderByDistanceFrom makes the store sort by proximity, so the first hit is the winner when
        // overlapping regions match (identical 0m distances in containment mode fall back to store order).
        var hits = this.withinMeters is null
            ? await this.store.GeoIntersects<T>(point, orderByDistanceFrom: point, filter: this.filter, cancellationToken: cancellationToken).ConfigureAwait(false)
            : await this.store.GeoWithinDistance<T>(point, this.withinMeters.Value, orderByDistanceFrom: point, filter: this.filter, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (hits.Count == 0)
            return null;

        var hit = hits[0];
        return new RegionMatch(this.idSelector(hit.Document), this.nameSelector?.Invoke(hit.Document), hit.Document, hit.DistanceMeters);
    }


    /// <inheritdoc />
    public async Task<RegionMatch?> GetRegion(string regionId, CancellationToken cancellationToken = default)
    {
        var doc = await this.store.Get<T>(regionId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return doc is null ? null : new RegionMatch(this.idSelector(doc), this.nameSelector?.Invoke(doc), doc);
    }
}
