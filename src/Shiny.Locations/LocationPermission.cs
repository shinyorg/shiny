namespace Shiny.Locations;

/// <summary>
/// The detailed result of requesting location permissions, including background and precise-accuracy capabilities.
/// </summary>
/// <param name="Access">The overall access state granted by the user.</param>
/// <param name="HasBackground">Whether background location access was granted, or null if not applicable on the current platform.</param>
/// <param name="HasFineAccess">Whether precise/fine-grained location access was granted, or null if not applicable on the current platform.</param>
public record LocationPermissionResult(
    AccessState Access,
    bool? HasBackground,
    bool? HasFineAccess
);
