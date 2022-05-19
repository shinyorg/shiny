namespace Shiny.Locations;

public record GeofenceCurrentStatus(
    GeofenceRegion Region,
    GeofenceState Status
);