namespace Shiny.Locations;

public record LocationPermissionResult(
    AccessState Access,
    bool? HasBackground,
    bool? HasFineAccess
);
