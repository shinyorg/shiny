namespace Shiny.Calendar.Internals;

/// <summary>
/// The fetch instructions a <see cref="CalendarEventQuery"/> hands to the platform store.
/// <para>
/// Everything here is a HINT used to narrow the native read. The builder applies its in-memory
/// predicates, sorting and paging afterwards, so an implementation is free to return a superset - it
/// must never return fewer events than the hints allow.
/// </para>
/// </summary>
public class CalendarEventQueryDescriptor
{
    /// <summary>Native filter: only events on this calendar. Null means all calendars.</summary>
    public string? CalendarId { get; set; }

    /// <summary>Native filter: lower bound of the event window.</summary>
    public DateTimeOffset? StartAfter { get; set; }

    /// <summary>Native filter: upper bound of the event window.</summary>
    public DateTimeOffset? EndBefore { get; set; }

    public int? Skip { get; set; }
    public int? Take { get; set; }
}
