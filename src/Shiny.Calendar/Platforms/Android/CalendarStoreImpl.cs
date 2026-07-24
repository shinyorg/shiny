using Android.Content;
using Android.Database;
using Android.Provider;
using Shiny.Calendar.Internals;
using Application = Android.App.Application;
using CalendarsColumns = Android.Provider.CalendarContract.Calendars;
using EventsColumns = Android.Provider.CalendarContract.Events;
using RemindersColumns = Android.Provider.CalendarContract.Reminders;
using AttendeesColumns = Android.Provider.CalendarContract.Attendees;

namespace Shiny.Calendar;

public class CalendarStoreImpl(AndroidPlatform platform) : ICalendarStore
{
    static ContentResolver Resolver => Application.Context.ContentResolver!;

    // Android CalendarAccessLevel: >= Contributor (500) can modify events.
    const int ContributorAccess = 500;

    public AccessState GetCurrentAccess()
    {
        var read = Application.Context.CheckSelfPermission(Android.Manifest.Permission.ReadCalendar) == Android.Content.PM.Permission.Granted;
        var write = Application.Context.CheckSelfPermission(Android.Manifest.Permission.WriteCalendar) == Android.Content.PM.Permission.Granted;

        if (read && write)
            return AccessState.Available;

        return read || write ? AccessState.Restricted : AccessState.Denied;
    }

    public async Task<AccessState> RequestAccess(CalendarAccessType accessType = CalendarAccessType.ReadWrite, CancellationToken ct = default)
    {
        var result = await platform
            .RequestPermissions(ct, Android.Manifest.Permission.ReadCalendar, Android.Manifest.Permission.WriteCalendar)
            .ConfigureAwait(false);

        var read = result.IsGranted(Android.Manifest.Permission.ReadCalendar);
        var write = result.IsGranted(Android.Manifest.Permission.WriteCalendar);

        if (read && write)
            return AccessState.Available;

        return read || write ? AccessState.Restricted : AccessState.Denied;
    }

    // ── Calendars ────────────────────────────────────────────────────

    public Task<IReadOnlyList<Calendar>> GetAll(CancellationToken ct = default) => Task.Run(() =>
    {
        var results = ReadCalendars(null, null);
        return (IReadOnlyList<Calendar>)results;
    }, ct);

    public Task<Calendar?> GetById(string calendarId, CancellationToken ct = default) => Task.Run(() =>
    {
        var results = ReadCalendars(CalendarsColumns.InterfaceConsts.Id + " = ?", [calendarId]);
        return results.Count > 0 ? results[0] : null;
    }, ct);

    public Task<string> Create(string name, string? color = null, CancellationToken ct = default) => Task.Run(() =>
    {
        var values = new ContentValues();
        values.Put(CalendarsColumns.InterfaceConsts.CalendarDisplayName, name);
        values.Put(CalendarsColumns.Name, name);
        values.Put(CalendarsColumns.InterfaceConsts.AccountName, name);
        values.Put(CalendarsColumns.InterfaceConsts.AccountType, CalendarContract.AccountTypeLocal);
        values.Put(CalendarsColumns.InterfaceConsts.CalendarAccessLevel, (int)CalendarAccess.AccessOwner);
        values.Put(CalendarsColumns.InterfaceConsts.OwnerAccount, name);
        var argb = ParseColor(color);
        if (argb.HasValue)
            values.Put(CalendarsColumns.InterfaceConsts.CalendarColor, argb.Value);

        // Calendar inserts must be made as a sync adapter.
        var uri = AsSyncAdapter(CalendarsColumns.ContentUri!, name, CalendarContract.AccountTypeLocal);
        var result = Resolver.Insert(uri, values)
            ?? throw new InvalidOperationException("Failed to create calendar.");

        return ContentUris.ParseId(result).ToString();
    }, ct);

    public Task Update(string calendarId, string newName, string? newColor = null, CancellationToken ct = default) => Task.Run(() =>
    {
        var values = new ContentValues();
        values.Put(CalendarsColumns.InterfaceConsts.CalendarDisplayName, newName);
        var argb = ParseColor(newColor);
        if (argb.HasValue)
            values.Put(CalendarsColumns.InterfaceConsts.CalendarColor, argb.Value);

        var uri = ContentUris.WithAppendedId(CalendarsColumns.ContentUri!, long.Parse(calendarId));
        Resolver.Update(uri!, values, null, null);
    }, ct);

    public Task Delete(string calendarId, CancellationToken ct = default) => Task.Run(() =>
    {
        var uri = ContentUris.WithAppendedId(CalendarsColumns.ContentUri!, long.Parse(calendarId));
        Resolver.Delete(uri!, null, null);
    }, ct);

    // ── Events ───────────────────────────────────────────────────────

    public Task<IReadOnlyList<CalendarEvent>> GetEvents(
        string? calendarId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken ct = default
    ) => Task.Run(() =>
    {
        var results = ReadEvents(calendarId, start, end);
        return (IReadOnlyList<CalendarEvent>)results;
    }, ct);

    public Task<CalendarEvent?> GetEvent(string eventId, CancellationToken ct = default) => Task.Run(() =>
    {
        var results = ReadEventsBySelection(EventsColumns.InterfaceConsts.Id + " = ?", [eventId]);
        return results.Count > 0 ? results[0] : null;
    }, ct);

    public IQueryable<CalendarEvent> Query()
        => new CalendarEventQueryable(new CalendarEventQueryProvider(ExecuteQuery));

    IEnumerable<CalendarEvent> ExecuteQuery(CalendarEventQueryDescriptor descriptor)
        => ReadEvents(descriptor.CalendarId, descriptor.StartAfter, descriptor.EndBefore);

    public Task<string> CreateEvent(CalendarEvent calendarEvent, CancellationToken ct = default) => Task.Run(() =>
    {
        var calendarId = ResolveTargetCalendarId(calendarEvent.CalendarId);
        var values = ToContentValues(calendarEvent, calendarId);

        var uri = Resolver.Insert(EventsColumns.ContentUri!, values)
            ?? throw new InvalidOperationException("Failed to create event.");
        var eventId = ContentUris.ParseId(uri);

        WriteReminders(eventId, calendarEvent.Reminders);
        WriteAttendees(eventId, calendarEvent.Attendees);

        return eventId.ToString();
    }, ct);

    public Task UpdateEvent(CalendarEvent calendarEvent, CancellationToken ct = default) => Task.Run(() =>
    {
        if (string.IsNullOrWhiteSpace(calendarEvent.Id))
            throw new ArgumentException("Event Id is required for update.", nameof(calendarEvent));

        var eventId = long.Parse(calendarEvent.Id);
        var calendarId = ResolveTargetCalendarId(calendarEvent.CalendarId);
        var values = ToContentValues(calendarEvent, calendarId);

        var uri = ContentUris.WithAppendedId(EventsColumns.ContentUri!, eventId);
        Resolver.Update(uri!, values, null, null);

        // Replace child reminders/attendees
        Resolver.Delete(RemindersColumns.ContentUri!, RemindersColumns.InterfaceConsts.EventId + " = ?", [eventId.ToString()]);
        Resolver.Delete(AttendeesColumns.ContentUri!, AttendeesColumns.InterfaceConsts.EventId + " = ?", [eventId.ToString()]);
        WriteReminders(eventId, calendarEvent.Reminders);
        WriteAttendees(eventId, calendarEvent.Attendees);
    }, ct);

    public Task DeleteEvent(string eventId, CancellationToken ct = default) => Task.Run(() =>
    {
        var uri = ContentUris.WithAppendedId(EventsColumns.ContentUri!, long.Parse(eventId));
        Resolver.Delete(uri!, null, null);
    }, ct);

    // ── Calendar reading ─────────────────────────────────────────────

    static List<Calendar> ReadCalendars(string? selection, string[]? args)
    {
        var results = new List<Calendar>();
        using var cursor = Resolver.Query(
            CalendarsColumns.ContentUri!,
            [
                CalendarsColumns.InterfaceConsts.Id,
                CalendarsColumns.InterfaceConsts.CalendarDisplayName,
                CalendarsColumns.InterfaceConsts.CalendarColor,
                CalendarsColumns.InterfaceConsts.CalendarAccessLevel,
                CalendarsColumns.InterfaceConsts.OwnerAccount
            ],
            selection,
            args,
            null
        );

        if (cursor == null)
            return results;

        while (cursor.MoveToNext())
        {
            results.Add(new Calendar
            {
                Id = cursor.GetString(0),
                Name = cursor.GetString(1) ?? string.Empty,
                Color = cursor.IsNull(2) ? null : ToHex(cursor.GetInt(2)),
                IsReadOnly = cursor.GetInt(3) < ContributorAccess,
                Account = cursor.GetString(4)
            });
        }
        return results;
    }

    // ── Event reading ────────────────────────────────────────────────

    List<CalendarEvent> ReadEvents(string? calendarId, DateTimeOffset? start, DateTimeOffset? end)
    {
        var (from, to) = ResolveWindow(start, end);
        var clauses = new List<string>
        {
            EventsColumns.InterfaceConsts.Dtstart + " <= ?",
            EventsColumns.InterfaceConsts.Dtend + " >= ?"
        };
        var args = new List<string> { to.ToUnixTimeMilliseconds().ToString(), from.ToUnixTimeMilliseconds().ToString() };

        if (!string.IsNullOrWhiteSpace(calendarId))
        {
            clauses.Add(EventsColumns.InterfaceConsts.CalendarId + " = ?");
            args.Add(calendarId);
        }

        return ReadEventsBySelection(string.Join(" AND ", clauses), args.ToArray());
    }

    List<CalendarEvent> ReadEventsBySelection(string? selection, string[]? args)
    {
        var events = new Dictionary<long, CalendarEvent>();

        using (var cursor = Resolver.Query(
            EventsColumns.ContentUri!,
            [
                EventsColumns.InterfaceConsts.Id,
                EventsColumns.InterfaceConsts.CalendarId,
                EventsColumns.InterfaceConsts.Title,
                EventsColumns.InterfaceConsts.Description,
                EventsColumns.InterfaceConsts.EventLocation,
                EventsColumns.InterfaceConsts.Dtstart,
                EventsColumns.InterfaceConsts.Dtend,
                EventsColumns.InterfaceConsts.AllDay,
                EventsColumns.InterfaceConsts.Availability,
                EventsColumns.InterfaceConsts.Rrule
            ],
            selection,
            args,
            EventsColumns.InterfaceConsts.Dtstart + " ASC"))
        {
            if (cursor != null)
            {
                while (cursor.MoveToNext())
                {
                    var id = cursor.GetLong(0);
                    var rrule = cursor.GetString(9);
                    events[id] = new CalendarEvent
                    {
                        Id = id.ToString(),
                        CalendarId = cursor.GetString(1),
                        Title = cursor.GetString(2) ?? string.Empty,
                        Description = cursor.GetString(3),
                        Location = cursor.GetString(4),
                        Start = DateTimeOffset.FromUnixTimeMilliseconds(cursor.GetLong(5)),
                        End = DateTimeOffset.FromUnixTimeMilliseconds(cursor.GetLong(6)),
                        IsAllDay = cursor.GetInt(7) == 1,
                        Availability = FromAndroidAvailability(cursor.GetInt(8)),
                        IsRecurring = !string.IsNullOrWhiteSpace(rrule),
                        RecurrenceRule = string.IsNullOrWhiteSpace(rrule) ? null : rrule
                    };
                }
            }
        }

        if (events.Count == 0)
            return [];

        LoadReminders(events);
        LoadAttendees(events);
        return events.Values.ToList();
    }

    static void LoadReminders(Dictionary<long, CalendarEvent> events)
    {
        var ids = events.Keys.ToList();
        var placeholders = string.Join(",", ids.Select(_ => "?"));
        using var cursor = Resolver.Query(
            RemindersColumns.ContentUri!,
            [RemindersColumns.InterfaceConsts.EventId, RemindersColumns.InterfaceConsts.Minutes],
            RemindersColumns.InterfaceConsts.EventId + " IN (" + placeholders + ")",
            ids.Select(i => i.ToString()).ToArray(),
            null
        );
        if (cursor == null)
            return;

        while (cursor.MoveToNext())
        {
            var eventId = cursor.GetLong(0);
            var minutes = cursor.GetInt(1);
            if (events.TryGetValue(eventId, out var evt) && minutes >= 0)
                evt.Reminders.Add(new EventReminder(TimeSpan.FromMinutes(minutes)));
        }
    }

    static void LoadAttendees(Dictionary<long, CalendarEvent> events)
    {
        var ids = events.Keys.ToList();
        var placeholders = string.Join(",", ids.Select(_ => "?"));
        using var cursor = Resolver.Query(
            AttendeesColumns.ContentUri!,
            [
                AttendeesColumns.InterfaceConsts.EventId,
                AttendeesColumns.InterfaceConsts.AttendeeName,
                AttendeesColumns.InterfaceConsts.AttendeeEmail,
                AttendeesColumns.InterfaceConsts.AttendeeRelationship,
                AttendeesColumns.InterfaceConsts.AttendeeStatus,
                AttendeesColumns.InterfaceConsts.AttendeeType
            ],
            AttendeesColumns.InterfaceConsts.EventId + " IN (" + placeholders + ")",
            ids.Select(i => i.ToString()).ToArray(),
            null
        );
        if (cursor == null)
            return;

        while (cursor.MoveToNext())
        {
            var eventId = cursor.GetLong(0);
            if (!events.TryGetValue(eventId, out var evt))
                continue;

            var relationship = cursor.GetInt(3);
            var attendee = new EventAttendee
            {
                Name = cursor.GetString(1),
                Email = cursor.GetString(2),
                Role = FromAndroidAttendeeType(cursor.GetInt(5)),
                Status = FromAndroidAttendeeStatus(cursor.GetInt(4)),
                IsOrganizer = relationship == (int)CalendarAttendeesRelationship.Organizer
            };
            evt.Attendees.Add(attendee);
            if (attendee.IsOrganizer)
                evt.Organizer = attendee;
        }
    }

    // ── Event writing ────────────────────────────────────────────────

    static ContentValues ToContentValues(CalendarEvent evt, long calendarId)
    {
        var values = new ContentValues();
        values.Put(EventsColumns.InterfaceConsts.CalendarId, calendarId);
        values.Put(EventsColumns.InterfaceConsts.Title, evt.Title);
        values.Put(EventsColumns.InterfaceConsts.Description, evt.Description);
        values.Put(EventsColumns.InterfaceConsts.EventLocation, evt.Location);
        values.Put(EventsColumns.InterfaceConsts.Dtstart, evt.Start.ToUnixTimeMilliseconds());
        values.Put(EventsColumns.InterfaceConsts.Dtend, evt.End.ToUnixTimeMilliseconds());
        values.Put(EventsColumns.InterfaceConsts.AllDay, evt.IsAllDay ? 1 : 0);
        values.Put(EventsColumns.InterfaceConsts.Availability, ToAndroidAvailability(evt.Availability));
        // EventTimezone is mandatory on insert.
        values.Put(EventsColumns.InterfaceConsts.EventTimezone, Java.Util.TimeZone.Default!.ID);
        return values;
    }

    static void WriteReminders(long eventId, List<EventReminder> reminders)
    {
        foreach (var reminder in reminders)
        {
            var values = new ContentValues();
            values.Put(RemindersColumns.InterfaceConsts.EventId, eventId);
            values.Put(RemindersColumns.InterfaceConsts.Minutes, (int)reminder.Offset.TotalMinutes);
            values.Put(RemindersColumns.InterfaceConsts.Method, (int)RemindersMethod.Alert);
            Resolver.Insert(RemindersColumns.ContentUri!, values);
        }
    }

    static void WriteAttendees(long eventId, List<EventAttendee> attendees)
    {
        foreach (var attendee in attendees)
        {
            var values = new ContentValues();
            values.Put(AttendeesColumns.InterfaceConsts.EventId, eventId);
            values.Put(AttendeesColumns.InterfaceConsts.AttendeeName, attendee.Name);
            values.Put(AttendeesColumns.InterfaceConsts.AttendeeEmail, attendee.Email);
            values.Put(AttendeesColumns.InterfaceConsts.AttendeeType, ToAndroidAttendeeType(attendee.Role));
            values.Put(AttendeesColumns.InterfaceConsts.AttendeeRelationship, (int)(attendee.IsOrganizer
                ? CalendarAttendeesRelationship.Organizer
                : CalendarAttendeesRelationship.Attendee));
            Resolver.Insert(AttendeesColumns.ContentUri!, values);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────

    long ResolveTargetCalendarId(string? calendarId)
    {
        if (!string.IsNullOrWhiteSpace(calendarId) && long.TryParse(calendarId, out var id))
            return id;

        // Fall back to the first writable calendar.
        var writable = ReadCalendars(null, null).FirstOrDefault(c => !c.IsReadOnly)
            ?? ReadCalendars(null, null).FirstOrDefault()
            ?? throw new InvalidOperationException("No calendar available to create the event in.");
        return long.Parse(writable.Id!);
    }

    static Android.Net.Uri AsSyncAdapter(Android.Net.Uri uri, string account, string accountType)
        => uri.BuildUpon()!
            .AppendQueryParameter(CalendarContract.CallerIsSyncadapter, "true")
            .AppendQueryParameter(CalendarsColumns.InterfaceConsts.AccountName, account)
            .AppendQueryParameter(CalendarsColumns.InterfaceConsts.AccountType, accountType)
            .Build()!;

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

    static int? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;
        var s = hex.TrimStart('#');
        if (s.Length != 6 || !int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
            return null;
        return unchecked((int)(0xFF000000 | (uint)rgb));
    }

    static string ToHex(int argb)
        => $"#{(argb >> 16) & 0xFF:X2}{(argb >> 8) & 0xFF:X2}{argb & 0xFF:X2}";

    static EventAvailability FromAndroidAvailability(int value) => value switch
    {
        (int)EventsAvailability.Free => EventAvailability.Free,
        (int)EventsAvailability.Tentative => EventAvailability.Tentative,
        _ => EventAvailability.Busy
    };

    static int ToAndroidAvailability(EventAvailability availability) => availability switch
    {
        EventAvailability.Free => (int)EventsAvailability.Free,
        EventAvailability.Tentative => (int)EventsAvailability.Tentative,
        _ => (int)EventsAvailability.Busy
    };

    static AttendeeRole FromAndroidAttendeeType(int value) => value switch
    {
        (int)CalendarAttendeesColumn.Required => AttendeeRole.Required,
        (int)CalendarAttendeesColumn.Optional => AttendeeRole.Optional,
        (int)CalendarAttendeesColumn.Resource => AttendeeRole.Resource,
        _ => AttendeeRole.Unknown
    };

    static int ToAndroidAttendeeType(AttendeeRole role) => role switch
    {
        AttendeeRole.Required => (int)CalendarAttendeesColumn.Required,
        AttendeeRole.Optional => (int)CalendarAttendeesColumn.Optional,
        AttendeeRole.Resource => (int)CalendarAttendeesColumn.Resource,
        _ => (int)CalendarAttendeesColumn.None
    };

    static AttendeeStatus FromAndroidAttendeeStatus(int value) => value switch
    {
        (int)CalendarAttendeesStatus.Accepted => AttendeeStatus.Accepted,
        (int)CalendarAttendeesStatus.Declined => AttendeeStatus.Declined,
        (int)CalendarAttendeesStatus.Invited => AttendeeStatus.Pending,
        (int)CalendarAttendeesStatus.Tentative => AttendeeStatus.Tentative,
        _ => AttendeeStatus.Unknown
    };
}
