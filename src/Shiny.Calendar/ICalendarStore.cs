namespace Shiny.Calendar;

public interface ICalendarStore
{
    /// <summary>
    /// Returns the current calendar access/permission state without prompting the user.
    /// </summary>
    AccessState GetCurrentAccess();

    /// <summary>
    /// Requests calendar access from the user (prompting if necessary) and returns the resulting state.
    /// </summary>
    /// <param name="accessType">The level of access required. iOS 17+ distinguishes read-only, write-only, and full access.</param>
    Task<AccessState> RequestAccess(CalendarAccessType accessType = CalendarAccessType.ReadWrite, CancellationToken ct = default);

    // ── Calendars ────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves all calendars available on the device.
    /// </summary>
    Task<IReadOnlyList<Calendar>> GetAll(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single calendar by its platform identifier, or null if not found.
    /// </summary>
    Task<Calendar?> GetById(string calendarId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new calendar and returns the platform-assigned identifier.
    /// </summary>
    /// <param name="color">Optional colour as a hex string (e.g. <c>#FF3B30</c>).</param>
    Task<string> Create(string name, string? color = null, CancellationToken ct = default);

    /// <summary>
    /// Updates the name and/or colour of an existing calendar.
    /// </summary>
    Task Update(string calendarId, string newName, string? newColor = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes the calendar with the specified identifier.
    /// </summary>
    Task Delete(string calendarId, CancellationToken ct = default);

    // ── Events ───────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves events, optionally filtered by calendar and/or a date window. When no window is
    /// supplied a sensible default range is used (implementation-defined, typically the current month).
    /// </summary>
    Task<IReadOnlyList<CalendarEvent>> GetEvents(
        string? calendarId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken ct = default
    );

    /// <summary>
    /// Retrieves a single event by its platform identifier, or null if not found.
    /// </summary>
    Task<CalendarEvent?> GetEvent(string eventId, CancellationToken ct = default);

    /// <summary>
    /// Returns a LINQ-queryable source of events. <c>.Where()</c> filters on <see cref="CalendarEvent.CalendarId"/>
    /// and the <see cref="CalendarEvent.Start"/>/<see cref="CalendarEvent.End"/> date window are translated
    /// to native queries; all other predicates are applied in-memory.
    /// </summary>
    IQueryable<CalendarEvent> Query();

    /// <summary>
    /// Creates a new event and returns the platform-assigned identifier. Set <see cref="CalendarEvent.CalendarId"/>
    /// to target a specific calendar; otherwise the device default calendar is used.
    /// </summary>
    Task<string> CreateEvent(CalendarEvent calendarEvent, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing event. The event must have a valid Id.
    /// </summary>
    Task UpdateEvent(CalendarEvent calendarEvent, CancellationToken ct = default);

    /// <summary>
    /// Deletes the event with the specified identifier.
    /// </summary>
    Task DeleteEvent(string eventId, CancellationToken ct = default);
}
