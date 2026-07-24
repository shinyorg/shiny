namespace Shiny.Calendar;

/// <summary>
/// The level of access being requested from the platform calendar store.
/// </summary>
public enum CalendarAccessType
{
    /// <summary>Read-only access to calendars and events.</summary>
    ReadOnly,

    /// <summary>
    /// Write-only access (iOS 17+ <c>NSCalendarsWriteOnlyAccessUsageDescription</c>). Lets an app add
    /// events without seeing existing ones. Other platforms treat this as full read/write.
    /// </summary>
    WriteOnly,

    /// <summary>Full read and write access.</summary>
    ReadWrite
}

/// <summary>
/// Whether an event marks its time as busy or free on the owner's calendar.
/// </summary>
public enum EventAvailability
{
    Busy,
    Free,
    Tentative,
    Unavailable
}

/// <summary>
/// The role an attendee plays in an event.
/// </summary>
public enum AttendeeRole
{
    Required,
    Optional,
    Resource,
    Unknown
}

/// <summary>
/// The response status of an attendee for an event invitation.
/// </summary>
public enum AttendeeStatus
{
    Unknown,
    Pending,
    Accepted,
    Declined,
    Tentative
}
