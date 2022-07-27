namespace Shiny.Notifications;

public record IosConfiguration(
    /// <summary>
    /// This requires a special entitlement from Apple that is general disabled for anything but health & public safety alerts
    /// </summary>
    bool UseTimeSensitiveEntitlement = false,


    bool UseCriticalEntitlement = false
);
