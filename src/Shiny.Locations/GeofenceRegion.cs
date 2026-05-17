namespace Shiny.Locations;


/// <summary>
/// A circular geographic region monitored for enter and exit transitions.
/// </summary>
/// <param name="Identifier">A unique identifier for the region.</param>
/// <param name="Center">The center point of the region.</param>
/// <param name="Radius">The radius of the region around the center.</param>
/// <param name="SingleUse">If true, the region is removed from monitoring after the first transition.</param>
/// <param name="NotifyOnEntry">Whether the delegate should be invoked when the device enters the region.</param>
/// <param name="NotifyOnExit">Whether the delegate should be invoked when the device exits the region.</param>
public record GeofenceRegion(
    string Identifier,
    Position Center,
    Distance Radius,
    bool SingleUse = false,
    bool NotifyOnEntry = true,
    bool NotifyOnExit = true
) : Shiny.Support.Repositories.IRepositoryEntity;
