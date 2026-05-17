namespace Shiny.Notifications;

/// <summary>
/// The importance level of a notification channel, controlling how prominently the system surfaces its notifications.
/// </summary>
public enum ChannelImportance
{
    /// <summary>
    /// Minimal interruption: shown silently with no sound or peek.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Default behavior: makes a sound and appears in the notification shade.
    /// </summary>
    Normal = 2,

    /// <summary>
    /// Higher priority: makes a sound and may peek as a heads-up notification.
    /// </summary>
    High = 3,

    /// <summary>
    /// Highest priority: bypasses Do Not Disturb where supported and is always shown to the user.
    /// </summary>
    Critical = 4
}
