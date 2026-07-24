using Shiny.Calendar;
using Shiny.Calendar.Internals;

namespace Shiny.Calendar.Tests;

/// <summary>
/// In-memory <see cref="ICalendarStore"/> used to exercise the device-independent logic (LINQ
/// visitor/interpreter/provider and the AI tool surface) without a real platform backend. The
/// <see cref="Query"/> path wires up the real <see cref="CalendarEventQueryProvider"/> so tests verify
/// the actual expression translation; the executor honours the native fetch hints
/// (<see cref="CalendarEventQueryDescriptor.CalendarId"/> / <c>StartAfter</c> / <c>EndBefore</c>) so we
/// can assert they were extracted.
/// </summary>
public class FakeCalendarStore : ICalendarStore
{
    readonly List<Calendar> calendars = [];
    readonly List<CalendarEvent> events = [];
    int nextId = 1;

    /// <summary>Records the descriptor produced for the last Query() enumeration, for assertions.</summary>
    public CalendarEventQueryDescriptor? LastDescriptor { get; private set; }

    public FakeCalendarStore Seed(params CalendarEvent[] evts)
    {
        foreach (var e in evts)
        {
            e.Id ??= (this.nextId++).ToString();
            this.events.Add(e);
            // Keep the id generator ahead of any explicitly-seeded numeric ids.
            if (int.TryParse(e.Id, out var n) && n >= this.nextId)
                this.nextId = n + 1;
        }
        return this;
    }

    public FakeCalendarStore SeedCalendars(params Calendar[] cals)
    {
        this.calendars.AddRange(cals);
        return this;
    }

    public AccessState GetCurrentAccess() => AccessState.Available;
    public Task<AccessState> RequestAccess(CalendarAccessType accessType = CalendarAccessType.ReadWrite, CancellationToken ct = default)
        => Task.FromResult(AccessState.Available);

    public Task<IReadOnlyList<Calendar>> GetAll(CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<Calendar>)this.calendars.ToList());

    public Task<Calendar?> GetById(string calendarId, CancellationToken ct = default)
        => Task.FromResult(this.calendars.FirstOrDefault(c => c.Id == calendarId));

    public Task<string> Create(string name, string? color = null, CancellationToken ct = default)
    {
        var cal = new Calendar((this.nextId++).ToString(), name) { Color = color };
        this.calendars.Add(cal);
        return Task.FromResult(cal.Id!);
    }

    public Task Update(string calendarId, string newName, string? newColor = null, CancellationToken ct = default)
    {
        var cal = this.calendars.First(c => c.Id == calendarId);
        cal.Name = newName;
        if (newColor != null) cal.Color = newColor;
        return Task.CompletedTask;
    }

    public Task Delete(string calendarId, CancellationToken ct = default)
    {
        this.calendars.RemoveAll(c => c.Id == calendarId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CalendarEvent>> GetEvents(string? calendarId = null, DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken ct = default)
        => Task.FromResult((IReadOnlyList<CalendarEvent>)Fetch(calendarId, start, end).ToList());

    public Task<CalendarEvent?> GetEvent(string eventId, CancellationToken ct = default)
        => Task.FromResult(this.events.FirstOrDefault(e => e.Id == eventId));

    public IQueryable<CalendarEvent> Query()
        => new CalendarEventQueryable(new CalendarEventQueryProvider(descriptor =>
        {
            this.LastDescriptor = descriptor;
            return Fetch(descriptor.CalendarId, descriptor.StartAfter, descriptor.EndBefore);
        }));

    public Task<string> CreateEvent(CalendarEvent calendarEvent, CancellationToken ct = default)
    {
        calendarEvent.Id = (this.nextId++).ToString();
        this.events.Add(calendarEvent);
        return Task.FromResult(calendarEvent.Id);
    }

    public Task UpdateEvent(CalendarEvent calendarEvent, CancellationToken ct = default)
    {
        var idx = this.events.FindIndex(e => e.Id == calendarEvent.Id);
        if (idx < 0) throw new InvalidOperationException("not found");
        this.events[idx] = calendarEvent;
        return Task.CompletedTask;
    }

    public Task DeleteEvent(string eventId, CancellationToken ct = default)
    {
        this.events.RemoveAll(e => e.Id == eventId);
        return Task.CompletedTask;
    }

    // Mimics a native windowed fetch: applies only the hints a real backend would.
    IEnumerable<CalendarEvent> Fetch(string? calendarId, DateTimeOffset? startAfter, DateTimeOffset? endBefore)
    {
        IEnumerable<CalendarEvent> q = this.events;
        if (!string.IsNullOrWhiteSpace(calendarId))
            q = q.Where(e => e.CalendarId == calendarId);
        if (startAfter.HasValue)
            q = q.Where(e => e.End >= startAfter.Value);
        if (endBefore.HasValue)
            q = q.Where(e => e.Start <= endBefore.Value);
        return q.ToList();
    }
}
