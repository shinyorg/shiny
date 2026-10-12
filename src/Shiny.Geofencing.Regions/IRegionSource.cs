using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// Resolves positions to regions for one monitored region set. This is the storage/query seam of region
/// geofencing - implement it to back a set with any spatial store or lookup service (see Shiny.Geofencing.Regions.DocumentDb
/// for the SQLite document store provider).
/// </summary>
public interface IRegionSource
{
    /// <summary>
    /// Finds the region the position falls in. When regions overlap, return the one that should win
    /// (normally the nearest) - the monitor only tracks one current region per set.
    /// </summary>
    /// <param name="position">The device position.</param>
    /// <param name="cancellationToken">Cancels the pending query.</param>
    /// <returns>The winning region, or null when the position is outside every region.</returns>
    Task<RegionMatch?> FindRegion(Position position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a region back by id. Used to describe an exit whose region is no longer held in memory
    /// (typically after the OS relaunched the app).
    /// </summary>
    /// <param name="regionId">The id previously returned in a <see cref="RegionMatch"/>.</param>
    /// <param name="cancellationToken">Cancels the pending query.</param>
    /// <returns>The region, or null when it no longer exists.</returns>
    Task<RegionMatch?> GetRegion(string regionId, CancellationToken cancellationToken = default);
}


/// <summary>
/// The region a position resolved to inside one region set.
/// </summary>
/// <param name="Id">
/// The region's stable id. It is persisted as the device's current region, so it must be the same value
/// <see cref="IRegionSource.GetRegion"/> accepts.
/// </param>
/// <param name="Name">An optional display name echoed on change events.</param>
/// <param name="Region">The region object itself (for example the stored document).</param>
/// <param name="DistanceMeters">Distance from the position to the region - 0 when the position is inside it.</param>
public record RegionMatch(string Id, string? Name, object Region, double DistanceMeters = 0);
