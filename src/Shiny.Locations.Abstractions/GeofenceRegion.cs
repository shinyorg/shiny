namespace Shiny.Locations;


public record GeofenceRegion(
    string Identifier,
    Position Center,
    Distance Radius,
    bool SingleUse = false,
    bool NotifyOnEntry = true,
    bool NotifyOnExit = true
) : Shiny.Support.Repositories.IRepositoryEntity;