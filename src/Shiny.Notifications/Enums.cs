using System;

/// <summary>
/// Capabilities that can be requested or queried when checking notification permissions.
/// </summary>
[Flags]
public enum AccessRequestFlags
{
    /// <summary>
    /// Standard local notification permission.
    /// </summary>
    Notification = 0,

    /// <summary>
    /// Permission required to fire notifications based on a geofence trigger.
    /// </summary>
    LocationAware = 1,

    /// <summary>
    /// Permission allowing notifications to break through Do Not Disturb and be marked time-sensitive (iOS).
    /// </summary>
    TimeSensitivity = 2,

    /// <summary>
    /// All available notification capabilities.
    /// </summary>
    All = Notification | LocationAware | TimeSensitivity
}


/// <summary>
/// The scope of notifications affected by a cancel operation.
/// </summary>
public enum CancelScope
{
    /// <summary>
    /// Cancel only notifications currently shown to the user.
    /// </summary>
    DisplayedOnly,

    /// <summary>
    /// Cancel only pending (scheduled, repeating, or geofence-triggered) notifications that have not yet fired.
    /// </summary>
    Pending,

    /// <summary>
    /// Cancel both displayed and pending notifications.
    /// </summary>
    All,
}
