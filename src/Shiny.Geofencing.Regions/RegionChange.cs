namespace Shiny.Locations;


/// <summary>
/// A geofence transition - the device entered or exited one region of one monitored set.
/// </summary>
/// <param name="RegionSet">The set the region belongs to.</param>
/// <param name="RegionId">The region's id.</param>
/// <param name="RegionName">The display name, when the source provides one.</param>
/// <param name="Region">
/// The region object. Null only when an exit is raised for a region that has since been deleted from its source.
/// </param>
/// <param name="Entered">True for an entry, false for an exit.</param>
/// <param name="Position">The GPS position that produced the transition.</param>
public record RegionChange(
    string RegionSet,
    string RegionId,
    string? RegionName,
    object? Region,
    bool Entered,
    Position Position
)
{
    /// <summary>
    /// The region cast to its type, or null when the type doesn't match (or the region was deleted before
    /// an exit could be described).
    /// </summary>
    public T? RegionAs<T>() where T : class => this.Region as T;
}


/// <summary>
/// The region the device is currently in for one monitored set. Returned by
/// <see cref="IRegionGeofenceManager.GetCurrent"/>.
/// </summary>
/// <param name="RegionSet">The set that was queried.</param>
/// <param name="RegionId">The matched region's id, or null when the device is outside every region in the set.</param>
/// <param name="RegionName">The display name, when the source provides one.</param>
/// <param name="Region">The matched region, or null when outside.</param>
/// <param name="DistanceMeters">Distance to the matched region - 0 when inside it.</param>
public record CurrentRegion(
    string RegionSet,
    string? RegionId,
    string? RegionName,
    object? Region,
    double DistanceMeters
)
{
    /// <summary>The region cast to its type, or null when outside / the type doesn't match.</summary>
    public T? RegionAs<T>() where T : class => this.Region as T;
}
