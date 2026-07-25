#if APPLE
using CoreFoundation;
using CoreGraphics;
using EventKit;
using Foundation;
using Shiny.Calendar.Internals;

namespace Shiny.Calendar;

/// <summary>
/// EventKit-backed calendar store shared across iOS, Mac Catalyst, and macOS.
/// </summary>
public class CalendarStoreImpl : ICalendarStore
{
    readonly EKEventStore store = new();

    public AccessState GetCurrentAccess()
    {
        var status = EKEventStore.GetAuthorizationStatus(EKEntityType.Event);
        return FromNative(status);
    }

    static AccessState FromNative(EKAuthorizationStatus status) => status switch
    {
        EKAuthorizationStatus.Authorized => AccessState.Available, // iOS 17+ full access reuses this value
        EKAuthorizationStatus.WriteOnly => AccessState.Restricted,
        EKAuthorizationStatus.Restricted => AccessState.Restricted,
        EKAuthorizationStatus.Denied => AccessState.Denied,
        _ => AccessState.Unknown
    };

    public Task<AccessState> RequestAccess(CalendarAccessType accessType = CalendarAccessType.ReadWrite, CancellationToken ct = default)
    {
        // Access is only ever decided once - iOS will not re-present the prompt after the user has
        // answered, so requesting again on a Denied/Restricted status just returns Denied without any UI.
        // Only run the request flow when the status is still undetermined.
        var current = EKEventStore.GetAuthorizationStatus(EKEntityType.Event);
        if (current != EKAuthorizationStatus.NotDetermined)
            return Task.FromResult(FromNative(current));

        var tcs = new TaskCompletionSource<AccessState>();

        void Handle(bool granted, NSError? error)
            => tcs.TrySetResult(error == null && granted ? AccessState.Available : AccessState.Denied);

        void Request()
        {
            try
            {
                var useNewApi =
                    OperatingSystem.IsIOSVersionAtLeast(17) ||
                    OperatingSystem.IsMacCatalystVersionAtLeast(17) ||
                    OperatingSystem.IsMacOSVersionAtLeast(14);

                if (useNewApi)
                {
                    if (accessType == CalendarAccessType.WriteOnly)
                        this.store.RequestWriteOnlyAccessToEvents(Handle);
                    else
                        this.store.RequestFullAccessToEvents(Handle);
                }
                else
                {
#pragma warning disable CA1422 // pre-iOS17 API
                    this.store.RequestAccess(EKEntityType.Event, Handle);
#pragma warning restore CA1422
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        // The EventKit TCC prompt must be presented from the main thread. RequestAccess may be called
        // from a background thread (e.g. a page-lifecycle hook), in which case the prompt silently fails
        // to appear and the completion returns "denied" - which is the classic "it never asked me" bug.
        if (NSThread.Current.IsMainThread)
            Request();
        else
            DispatchQueue.MainQueue.DispatchAsync(Request);

        return tcs.Task;
    }

    // ── Calendars ────────────────────────────────────────────────────

    public Task<IReadOnlyList<Calendar>> GetAll(CancellationToken ct = default)
    {
        var cals = this.store.GetCalendars(EKEntityType.Event) ?? [];
        IReadOnlyList<Calendar> results = cals.Select(ToCalendar).ToList();
        return Task.FromResult(results);
    }

    public Task<Calendar?> GetById(string calendarId, CancellationToken ct = default)
    {
        var cal = this.store.GetCalendar(calendarId);
        return Task.FromResult(cal == null ? null : ToCalendar(cal));
    }

    public Task<string> Create(string name, string? color = null, CancellationToken ct = default)
    {
        var cal = EKCalendar.Create(EKEntityType.Event, this.store);
        cal.Title = name;

        var source = this.store.DefaultCalendarForNewEvents?.Source
            ?? this.store.Sources?.FirstOrDefault(s => s.SourceType == EKSourceType.Local)
            ?? this.store.Sources?.FirstOrDefault();
        if (source != null)
            cal.Source = source;

        var cg = ToCGColor(color);
        if (cg != null)
            cal.CGColor = cg;

        if (!this.store.SaveCalendar(cal, true, out var error))
            throw new InvalidOperationException($"Failed to create calendar: {error?.LocalizedDescription}");

        return Task.FromResult(cal.CalendarIdentifier);
    }

    public Task Update(string calendarId, string newName, string? newColor = null, CancellationToken ct = default)
    {
        var cal = this.store.GetCalendar(calendarId)
            ?? throw new InvalidOperationException($"Calendar with Id '{calendarId}' not found.");

        cal.Title = newName;
        var cg = ToCGColor(newColor);
        if (cg != null)
            cal.CGColor = cg;

        if (!this.store.SaveCalendar(cal, true, out var error))
            throw new InvalidOperationException($"Failed to update calendar: {error?.LocalizedDescription}");

        return Task.CompletedTask;
    }

    public Task Delete(string calendarId, CancellationToken ct = default)
    {
        var cal = this.store.GetCalendar(calendarId)
            ?? throw new InvalidOperationException($"Calendar with Id '{calendarId}' not found.");

        if (!this.store.RemoveCalendar(cal, true, out var error))
            throw new InvalidOperationException($"Failed to delete calendar: {error?.LocalizedDescription}");

        return Task.CompletedTask;
    }

    // ── Events ───────────────────────────────────────────────────────

    public Task<IReadOnlyList<CalendarEvent>> GetEvents(
        string? calendarId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken ct = default
    )
    {
        IReadOnlyList<CalendarEvent> results = this.FetchEvents(calendarId, start, end).Select(ToEvent).ToList();
        return Task.FromResult(results);
    }

    public Task<CalendarEvent?> GetEvent(string eventId, CancellationToken ct = default)
    {
        var ev = this.store.EventFromIdentifier(eventId);
        return Task.FromResult(ev == null ? null : ToEvent(ev));
    }

    public IQueryable<CalendarEvent> Query()
        => new CalendarEventQueryable(new CalendarEventQueryProvider(this.ExecuteQuery));

    IEnumerable<CalendarEvent> ExecuteQuery(CalendarEventQueryDescriptor descriptor)
        => this.FetchEvents(descriptor.CalendarId, descriptor.StartAfter, descriptor.EndBefore).Select(ToEvent);

    EKEvent[] FetchEvents(string? calendarId, DateTimeOffset? start, DateTimeOffset? end)
    {
        var (from, to) = ResolveWindow(start, end);

        EKCalendar[]? calendars = null;
        if (!string.IsNullOrWhiteSpace(calendarId))
        {
            var cal = this.store.GetCalendar(calendarId);
            if (cal == null)
                return [];
            calendars = [cal];
        }

        var predicate = this.store.PredicateForEvents(ToNsDate(from), ToNsDate(to), calendars);
        return this.store.EventsMatching(predicate) ?? [];
    }

    public Task<string> CreateEvent(CalendarEvent calendarEvent, CancellationToken ct = default)
    {
        var ev = EKEvent.FromStore(this.store);
        this.PopulateEvent(ev, calendarEvent);

        if (!this.store.SaveEvent(ev, EKSpan.ThisEvent, out var error))
            throw new InvalidOperationException($"Failed to create event: {error?.LocalizedDescription}");

        return Task.FromResult(ev.EventIdentifier);
    }

    public Task UpdateEvent(CalendarEvent calendarEvent, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(calendarEvent.Id))
            throw new ArgumentException("Event Id is required for update.", nameof(calendarEvent));

        var ev = this.store.EventFromIdentifier(calendarEvent.Id)
            ?? throw new InvalidOperationException($"Event with Id '{calendarEvent.Id}' not found.");

        this.PopulateEvent(ev, calendarEvent);

        if (!this.store.SaveEvent(ev, EKSpan.ThisEvent, out var error))
            throw new InvalidOperationException($"Failed to update event: {error?.LocalizedDescription}");

        return Task.CompletedTask;
    }

    public Task DeleteEvent(string eventId, CancellationToken ct = default)
    {
        var ev = this.store.EventFromIdentifier(eventId)
            ?? throw new InvalidOperationException($"Event with Id '{eventId}' not found.");

        if (!this.store.RemoveEvent(ev, EKSpan.ThisEvent, true, out var error))
            throw new InvalidOperationException($"Failed to delete event: {error?.LocalizedDescription}");

        return Task.CompletedTask;
    }

    // ── Mapping ──────────────────────────────────────────────────────

    void PopulateEvent(EKEvent ev, CalendarEvent src)
    {
        ev.Title = src.Title ?? string.Empty;
        ev.Notes = src.Description;
        ev.Location = src.Location;
        ev.StartDate = ToNsDate(src.Start);
        ev.EndDate = ToNsDate(src.End);
        ev.AllDay = src.IsAllDay;
        ev.Availability = ToNativeAvailability(src.Availability);
        ev.Url = string.IsNullOrWhiteSpace(src.Url) ? null : new NSUrl(src.Url);

        // Target calendar
        EKCalendar? cal = null;
        if (!string.IsNullOrWhiteSpace(src.CalendarId))
            cal = this.store.GetCalendar(src.CalendarId);
        ev.Calendar = cal ?? this.store.DefaultCalendarForNewEvents ?? ev.Calendar;

        // Reminders (replace existing)
        if (ev.Alarms != null)
        {
            foreach (var existing in ev.Alarms)
                ev.RemoveAlarm(existing);
        }
        foreach (var reminder in src.Reminders)
            ev.AddAlarm(EKAlarm.FromTimeInterval(-reminder.Offset.TotalSeconds));

        // NOTE: EventKit does not allow setting attendees programmatically - src.Attendees is ignored on Apple.
    }

    static Calendar ToCalendar(EKCalendar cal) => new()
    {
        Id = cal.CalendarIdentifier,
        Name = cal.Title,
        Color = ToHex(cal.CGColor),
        IsReadOnly = !cal.AllowsContentModifications,
        Account = cal.Source?.Title
    };

    static CalendarEvent ToEvent(EKEvent ev)
    {
        var result = new CalendarEvent
        {
            Id = ev.EventIdentifier,
            CalendarId = ev.Calendar?.CalendarIdentifier,
            Title = ev.Title ?? string.Empty,
            Description = ev.Notes,
            Location = ev.Location,
            Start = ToDto(ev.StartDate),
            End = ToDto(ev.EndDate),
            IsAllDay = ev.AllDay,
            Availability = FromNativeAvailability(ev.Availability),
            Url = ev.Url?.AbsoluteString,
            IsRecurring = ev.HasRecurrenceRules,
            RecurrenceRule = ev.HasRecurrenceRules ? ev.RecurrenceRules?.FirstOrDefault()?.ToString() : null
        };

        if (ev.Alarms != null)
        {
            foreach (var alarm in ev.Alarms)
                result.Reminders.Add(new EventReminder(TimeSpan.FromSeconds(-alarm.RelativeOffset)));
        }

        if (ev.Attendees != null)
        {
            foreach (var p in ev.Attendees)
                result.Attendees.Add(ToAttendee(p));
        }

        if (ev.Organizer != null)
            result.Organizer = ToAttendee(ev.Organizer);

        return result;
    }

    static EventAttendee ToAttendee(EKParticipant p) => new()
    {
        Name = p.Name,
        Email = ExtractEmail(p),
        Role = p.ParticipantRole switch
        {
            EKParticipantRole.Required => AttendeeRole.Required,
            EKParticipantRole.Optional => AttendeeRole.Optional,
            EKParticipantRole.NonParticipant => AttendeeRole.Resource,
            _ => AttendeeRole.Unknown
        },
        Status = p.ParticipantStatus switch
        {
            EKParticipantStatus.Pending => AttendeeStatus.Pending,
            EKParticipantStatus.Accepted => AttendeeStatus.Accepted,
            EKParticipantStatus.Declined => AttendeeStatus.Declined,
            EKParticipantStatus.Tentative => AttendeeStatus.Tentative,
            _ => AttendeeStatus.Unknown
        }
    };

    static string? ExtractEmail(EKParticipant p)
    {
        // The participant URL is typically a mailto: URI.
        var url = p.Url?.AbsoluteString;
        if (string.IsNullOrWhiteSpace(url))
            return null;
        return url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ? url.Substring(7) : url;
    }

    static EventAvailability FromNativeAvailability(EKEventAvailability a) => a switch
    {
        EKEventAvailability.Busy => EventAvailability.Busy,
        EKEventAvailability.Free => EventAvailability.Free,
        EKEventAvailability.Tentative => EventAvailability.Tentative,
        EKEventAvailability.Unavailable => EventAvailability.Unavailable,
        _ => EventAvailability.Busy
    };

    static EKEventAvailability ToNativeAvailability(EventAvailability a) => a switch
    {
        EventAvailability.Busy => EKEventAvailability.Busy,
        EventAvailability.Free => EKEventAvailability.Free,
        EventAvailability.Tentative => EKEventAvailability.Tentative,
        EventAvailability.Unavailable => EKEventAvailability.Unavailable,
        _ => EKEventAvailability.Busy
    };

    // ── Helpers ──────────────────────────────────────────────────────

    static (DateTimeOffset From, DateTimeOffset To) ResolveWindow(DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start.HasValue && end.HasValue)
            return (start.Value, end.Value);
        if (start.HasValue)
            return (start.Value, start.Value.AddYears(1));
        if (end.HasValue)
            return (end.Value.AddYears(-1), end.Value);

        var now = DateTimeOffset.Now;
        return (now.AddMonths(-1), now.AddMonths(3));
    }

    static NSDate ToNsDate(DateTimeOffset dto) => (NSDate)dto.UtcDateTime;

    static DateTimeOffset ToDto(NSDate? date)
    {
        if (date == null)
            return default;
        return new DateTimeOffset((DateTime)date, TimeSpan.Zero);
    }

    static string? ToHex(CGColor? color)
    {
        var comps = color?.Components;
        if (comps == null || comps.Length < 3)
            return null;

        int r = (int)Math.Round(comps[0] * 255);
        int g = (int)Math.Round(comps[1] * 255);
        int b = (int)Math.Round(comps[2] * 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    static CGColor? ToCGColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;

        var s = hex.TrimStart('#');
        if (s.Length != 6 || !int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
            return null;

        var r = ((rgb >> 16) & 0xFF) / 255f;
        var g = ((rgb >> 8) & 0xFF) / 255f;
        var b = (rgb & 0xFF) / 255f;
        return new CGColor(r, g, b, 1f);
    }
}
#endif
