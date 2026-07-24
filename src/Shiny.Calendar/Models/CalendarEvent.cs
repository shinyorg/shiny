namespace Shiny.Calendar;

/// <summary>
/// An event on a device calendar.
/// </summary>
public class CalendarEvent
{
    public CalendarEvent() { }

    public CalendarEvent(string title, DateTimeOffset start, DateTimeOffset end)
    {
        this.Title = title;
        this.Start = start;
        this.End = end;
    }

    /// <summary>The platform-assigned event identifier.</summary>
    public string? Id { get; set; }

    /// <summary>The identifier of the calendar this event belongs to.</summary>
    public string? CalendarId { get; set; }

    /// <summary>The event title / subject.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>The free-text description / notes for the event.</summary>
    public string? Description { get; set; }

    /// <summary>The event location.</summary>
    public string? Location { get; set; }

    /// <summary>When the event starts.</summary>
    public DateTimeOffset Start { get; set; }

    /// <summary>When the event ends.</summary>
    public DateTimeOffset End { get; set; }

    /// <summary>True for all-day events (time components are ignored).</summary>
    public bool IsAllDay { get; set; }

    /// <summary>How the event marks the owner's availability.</summary>
    public EventAvailability Availability { get; set; } = EventAvailability.Busy;

    /// <summary>An optional URL associated with the event.</summary>
    public string? Url { get; set; }

    /// <summary>True when the event is part of a recurring series. Recurrence is read-only.</summary>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// A human/ICS-style description of the recurrence rule when the event recurs, or null. Read-only -
    /// recurrence rules are surfaced but not written back across platforms.
    /// </summary>
    public string? RecurrenceRule { get; set; }

    /// <summary>Reminders/alarms attached to the event.</summary>
    public List<EventReminder> Reminders { get; set; } = [];

    /// <summary>The event attendees. Attendee writes are honoured on Android; ignored on Apple (EventKit limitation).</summary>
    public List<EventAttendee> Attendees { get; set; } = [];

    /// <summary>The event organizer, when available. Read-only.</summary>
    public EventAttendee? Organizer { get; set; }

    public override string ToString() => this.Title;
}
