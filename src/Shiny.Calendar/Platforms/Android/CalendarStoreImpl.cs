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

    readonly object changedLock = new();
    EventHandler? changed;
    ChangeObserver? changeObserver;

    public event EventHandler? Changed
    {
        add
        {
            lock (this.changedLock)
            {
                this.changed += value;
                if (this.changeObserver != null)
                    return;

                // The provider root with descendants covers the calendars, events, instances and reminders tables.
                var observer = new ChangeObserver(() => this.changed?.Invoke(this, EventArgs.Empty));
                try
                {
                    Resolver.RegisterContentObserver(CalendarContract.ContentUri!, true, observer);
                    this.changeObserver = observer;
                }
                catch (Java.Lang.SecurityException)
                {
                    // No calendar permission yet - the next subscription tries again.
                    observer.Dispose();
                }
            }
        }
        remove
        {
            lock (this.changedLock)
            {
                this.changed -= value;
                if (this.changed == null && this.changeObserver != null)
                {
                    Resolver.UnregisterContentObserver(this.changeObserver);
                    this.changeObserver.Dispose();
                    this.changeObserver = null;
                }
            }
        }
    }

    // A null handler delivers OnChange on a binder thread.
    sealed class ChangeObserver(Action onChange) : ContentObserver(null)
    {
        public override void OnChange(bool selfChange) => onChange();
    }

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

    // SQLite before 3.32 (Android 11 and older) caps a statement at 999 bound variables.
    const int MaxSqlArgs = 500;

    public Task<IReadOnlyList<CalendarEvent>> GetEvents(
        string? calendarId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken ct = default
    ) => Task.Run(() =>
    {
        var results = ReadInstances(calendarId, start, end);
        return (IReadOnlyList<CalendarEvent>)results;
    }, ct);

    public Task<CalendarEvent?> GetEvent(string eventId, CancellationToken ct = default) => Task.Run(() =>
    {
        var (id, occurrence) = EventOccurrenceId.Decode(eventId);
        if (occurrence != null)
            return ReadOccurrence(id, occurrence.Value);

        var results = ReadEventsBySelection(EventsColumns.InterfaceConsts.Id + " = ?", [id]);
        return results.Count > 0 ? results[0] : null;
    }, ct);

    public CalendarEventQuery Query() => new((descriptor, ct) => Task.Run(
        () => (IEnumerable<CalendarEvent>)ReadInstances(descriptor.CalendarId, descriptor.StartAfter, descriptor.EndBefore),
        ct
    ));

    public Task<string> CreateEvent(CalendarEvent calendarEvent, CancellationToken ct = default) => Task.Run(() =>
    {
        var calendarId = ResolveTargetCalendarId(calendarEvent.CalendarId);
        var values = ToContentValues(calendarEvent, false, null);
        values.Put(EventsColumns.InterfaceConsts.CalendarId, calendarId);

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

        var (id, occurrence) = EventOccurrenceId.Decode(calendarEvent.Id);
        var eventId = long.Parse(id);
        var row = ReadEventRow(eventId)
            ?? throw new InvalidOperationException($"Event with Id '{calendarEvent.Id}' not found.");

        if (occurrence != null && row.IsRecurring)
        {
            // one occurrence of a series: an exception row against its original start replaces just that instance
            var exceptionValues = ToContentValues(calendarEvent, false, row.Timezone);
            exceptionValues.Put(EventsColumns.InterfaceConsts.OriginalInstanceTime, occurrence.Value.ToUnixTimeMilliseconds());
            exceptionValues.PutNull(EventsColumns.InterfaceConsts.Duration);
            exceptionValues.PutNull(EventsColumns.InterfaceConsts.Rrule);

            var exceptionUri = ContentUris.WithAppendedId(EventsColumns.ContentExceptionUri!, eventId);
            var inserted = Resolver.Insert(exceptionUri!, exceptionValues)
                ?? throw new InvalidOperationException("Failed to update the event occurrence.");
            var exceptionId = ContentUris.ParseId(inserted);

            ReplaceReminders(exceptionId, calendarEvent.Reminders);
            ReplaceAttendees(exceptionId, calendarEvent.Attendees);
            return;
        }

        // a series keeps its RRULE and must carry DURATION rather than DTEND
        var values = ToContentValues(calendarEvent, row.IsRecurring, row.Timezone);
        if (!string.IsNullOrWhiteSpace(calendarEvent.CalendarId))
            values.Put(EventsColumns.InterfaceConsts.CalendarId, ResolveTargetCalendarId(calendarEvent.CalendarId));

        var uri = ContentUris.WithAppendedId(EventsColumns.ContentUri!, eventId);
        Resolver.Update(uri!, values, null, null);

        ReplaceReminders(eventId, calendarEvent.Reminders);
        ReplaceAttendees(eventId, calendarEvent.Attendees);
    }, ct);

    public Task DeleteEvent(string eventId, bool deleteSeries = false, CancellationToken ct = default) => Task.Run(() =>
    {
        var (rawId, occurrence) = EventOccurrenceId.Decode(eventId);
        var id = long.Parse(rawId);
        var row = ReadEventRow(id)
            ?? throw new InvalidOperationException($"Event with Id '{eventId}' not found.");

        var uri = ContentUris.WithAppendedId(EventsColumns.ContentUri!, id);
        if (!row.IsRecurring)
        {
            Resolver.Delete(uri!, null, null);
            return;
        }

        // a series id (rather than an occurrence id) stands for the series' first occurrence
        var instanceTime = occurrence?.ToUnixTimeMilliseconds() ?? row.Dtstart;

        if (deleteSeries)
        {
            if (instanceTime <= row.Dtstart)
            {
                Resolver.Delete(uri!, null, null);
                return;
            }

            // this and all future occurrences: end the series just before this one, and drop later exceptions
            var values = new ContentValues();
            values.Put(EventsColumns.InterfaceConsts.Rrule, RecurrenceRules.EndBefore(row.Rrule!, DateTimeOffset.FromUnixTimeMilliseconds(instanceTime), row.AllDay));
            Resolver.Update(uri!, values, null, null);
            Resolver.Delete(
                EventsColumns.ContentUri!,
                EventsColumns.InterfaceConsts.OriginalId + " = ? AND " + EventsColumns.InterfaceConsts.OriginalInstanceTime + " >= ?",
                [id.ToString(), instanceTime.ToString()]
            );
            return;
        }

        // cancelling a single occurrence means inserting a cancelled exception row against its original start time
        var cancel = new ContentValues();
        cancel.Put(EventsColumns.InterfaceConsts.OriginalInstanceTime, instanceTime);
        cancel.Put(EventsColumns.InterfaceConsts.Status, (int)EventsStatus.Canceled);

        var exceptionUri = ContentUris.WithAppendedId(EventsColumns.ContentExceptionUri!, id);
        Resolver.Insert(exceptionUri!, cancel);
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

    static readonly string[] EventColumns =
    [
        EventsColumns.InterfaceConsts.CalendarId,
        EventsColumns.InterfaceConsts.Title,
        EventsColumns.InterfaceConsts.Description,
        EventsColumns.InterfaceConsts.EventLocation,
        EventsColumns.InterfaceConsts.AllDay,
        EventsColumns.InterfaceConsts.Availability,
        EventsColumns.InterfaceConsts.Rrule
    ];

    // The Instances table expands recurring series into the occurrences inside the window (the Events table only
    // holds series masters, whose DTEND is NULL - recurring rows carry a DURATION instead).
    List<CalendarEvent> ReadInstances(string? calendarId, DateTimeOffset? start, DateTimeOffset? end)
    {
        var (from, to) = ResolveWindow(start, end);
        string? selection = null;
        string[]? args = null;
        if (!string.IsNullOrWhiteSpace(calendarId))
        {
            selection = EventsColumns.InterfaceConsts.CalendarId + " = ?";
            args = [calendarId];
        }
        return QueryInstances(from.ToUnixTimeMilliseconds(), to.ToUnixTimeMilliseconds(), selection, args);
    }

    CalendarEvent? ReadOccurrence(string eventId, DateTimeOffset originalStart)
    {
        var begin = originalStart.ToUnixTimeMilliseconds();
        var results = QueryInstances(
            begin,
            begin + 1,
            CalendarContract.Instances.EventId + " = ? AND " + CalendarContract.Instances.Begin + " = ?",
            [eventId, begin.ToString()]
        );
        return results.Count > 0 ? results[0] : null;
    }

    List<CalendarEvent> QueryInstances(long begin, long end, string? selection, string[]? args)
    {
        var builder = CalendarContract.Instances.ContentUri!.BuildUpon()!;
        ContentUris.AppendId(builder, begin);
        ContentUris.AppendId(builder, end);

        var events = new List<(long EventId, CalendarEvent Event)>();
        using (var cursor = Resolver.Query(
            builder.Build()!,
            [CalendarContract.Instances.EventId, CalendarContract.Instances.Begin, CalendarContract.Instances.End, .. EventColumns],
            selection,
            args,
            CalendarContract.Instances.Begin + " ASC"))
        {
            if (cursor != null)
            {
                while (cursor.MoveToNext())
                {
                    var eventId = cursor.GetLong(0);
                    var rawBegin = DateTimeOffset.FromUnixTimeMilliseconds(cursor.GetLong(1));
                    var rawEnd = DateTimeOffset.FromUnixTimeMilliseconds(cursor.GetLong(2));
                    var evt = ReadEventColumns(cursor, 3, rawBegin, rawEnd);
                    evt.Id = evt.IsRecurring
                        ? EventOccurrenceId.Encode(eventId.ToString(), rawBegin)
                        : eventId.ToString();
                    events.Add((eventId, evt));
                }
            }
        }
        LoadChildren(events);
        return events.Select(x => x.Event).ToList();
    }

    List<CalendarEvent> ReadEventsBySelection(string? selection, string[]? args)
    {
        var events = new List<(long EventId, CalendarEvent Event)>();

        using (var cursor = Resolver.Query(
            EventsColumns.ContentUri!,
            [
                EventsColumns.InterfaceConsts.Id,
                EventsColumns.InterfaceConsts.Dtstart,
                EventsColumns.InterfaceConsts.Dtend,
                EventsColumns.InterfaceConsts.Duration,
                .. EventColumns
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
                    var rawStart = DateTimeOffset.FromUnixTimeMilliseconds(cursor.GetLong(1));

                    // recurring rows have a NULL DTEND and a DURATION
                    var duration = RecurrenceDuration.Parse(cursor.GetString(3));
                    var rawEnd = !cursor.IsNull(2)
                        ? DateTimeOffset.FromUnixTimeMilliseconds(cursor.GetLong(2))
                        : rawStart + (duration ?? TimeSpan.Zero);

                    var evt = ReadEventColumns(cursor, 4, rawStart, rawEnd);
                    evt.Id = id.ToString();
                    events.Add((id, evt));
                }
            }
        }

        LoadChildren(events);
        return events.Select(x => x.Event).ToList();
    }

    static CalendarEvent ReadEventColumns(ICursor cursor, int offset, DateTimeOffset rawStart, DateTimeOffset rawEnd)
    {
        var allDay = cursor.GetInt(offset + 4) == 1;
        var rrule = cursor.GetString(offset + 6);
        return new CalendarEvent
        {
            CalendarId = cursor.GetString(offset),
            Title = cursor.GetString(offset + 1) ?? string.Empty,
            Description = cursor.GetString(offset + 2),
            Location = cursor.GetString(offset + 3),
            // all-day rows are UTC midnights; hand them out as local midnights, like EventKit does
            Start = allDay ? AllDayDates.FromStorage(rawStart) : rawStart,
            End = allDay ? AllDayDates.FromStorage(rawEnd) : rawEnd,
            IsAllDay = allDay,
            Availability = FromAndroidAvailability(cursor.GetInt(offset + 5)),
            IsRecurring = !string.IsNullOrWhiteSpace(rrule),
            RecurrenceRule = string.IsNullOrWhiteSpace(rrule) ? null : rrule
        };
    }

    static EventRow? ReadEventRow(long eventId)
    {
        using var cursor = Resolver.Query(
            EventsColumns.ContentUri!,
            [
                EventsColumns.InterfaceConsts.Rrule,
                EventsColumns.InterfaceConsts.Dtstart,
                EventsColumns.InterfaceConsts.AllDay,
                EventsColumns.InterfaceConsts.EventTimezone
            ],
            EventsColumns.InterfaceConsts.Id + " = ?",
            [eventId.ToString()],
            null
        );
        if (cursor == null || !cursor.MoveToFirst())
            return null;

        return new EventRow(cursor.GetString(0), cursor.GetLong(1), cursor.GetInt(2) == 1, cursor.GetString(3));
    }

    sealed record EventRow(string? Rrule, long Dtstart, bool AllDay, string? Timezone)
    {
        public bool IsRecurring => !string.IsNullOrWhiteSpace(this.Rrule);
    }

    static void LoadChildren(List<(long EventId, CalendarEvent Event)> events)
    {
        if (events.Count == 0)
            return;

        var byId = events.ToLookup(x => x.EventId, x => x.Event);
        foreach (var chunk in byId.Select(x => x.Key).Chunk(MaxSqlArgs))
        {
            LoadReminders(byId, chunk);
            LoadAttendees(byId, chunk);
        }
    }

    static string InClause(string column, long[] ids)
        => column + " IN (" + string.Join(",", ids.Select(_ => "?")) + ")";

    static void LoadReminders(ILookup<long, CalendarEvent> events, long[] ids)
    {
        using var cursor = Resolver.Query(
            RemindersColumns.ContentUri!,
            [RemindersColumns.InterfaceConsts.EventId, RemindersColumns.InterfaceConsts.Minutes],
            InClause(RemindersColumns.InterfaceConsts.EventId, ids),
            ids.Select(i => i.ToString()).ToArray(),
            null
        );
        if (cursor == null)
            return;

        while (cursor.MoveToNext())
        {
            var eventId = cursor.GetLong(0);
            var minutes = cursor.GetInt(1);
            if (minutes >= 0)
            {
                foreach (var evt in events[eventId])
                    evt.Reminders.Add(new EventReminder(TimeSpan.FromMinutes(minutes)));
            }
        }
    }

    static void LoadAttendees(ILookup<long, CalendarEvent> events, long[] ids)
    {
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
            InClause(AttendeesColumns.InterfaceConsts.EventId, ids),
            ids.Select(i => i.ToString()).ToArray(),
            null
        );
        if (cursor == null)
            return;

        while (cursor.MoveToNext())
        {
            var eventId = cursor.GetLong(0);
            var relationship = cursor.GetInt(3);
            foreach (var evt in events[eventId])
            {
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
    }

    // ── Event writing ────────────────────────────────────────────────

    static ContentValues ToContentValues(CalendarEvent evt, bool recurring, string? existingTimezone)
    {
        // all-day events must be stored as UTC midnights in the UTC zone, with an exclusive end
        var (start, end) = evt.IsAllDay
            ? AllDayDates.ToStorage(evt.Start, evt.End)
            : (evt.Start, evt.End);

        var values = new ContentValues();
        values.Put(EventsColumns.InterfaceConsts.Title, evt.Title);
        values.Put(EventsColumns.InterfaceConsts.Description, evt.Description);
        values.Put(EventsColumns.InterfaceConsts.EventLocation, evt.Location);
        values.Put(EventsColumns.InterfaceConsts.Dtstart, start.ToUnixTimeMilliseconds());
        if (recurring)
        {
            values.Put(EventsColumns.InterfaceConsts.Duration, RecurrenceDuration.ToRfc5545(end - start, evt.IsAllDay));
            values.PutNull(EventsColumns.InterfaceConsts.Dtend);
        }
        else
        {
            values.Put(EventsColumns.InterfaceConsts.Dtend, end.ToUnixTimeMilliseconds());
        }
        values.Put(EventsColumns.InterfaceConsts.AllDay, evt.IsAllDay ? 1 : 0);
        values.Put(EventsColumns.InterfaceConsts.Availability, ToAndroidAvailability(evt.Availability));
        // EventTimezone is mandatory on insert, and must be UTC for all-day events
        var timezone = evt.IsAllDay ? "UTC" : (existingTimezone ?? Java.Util.TimeZone.Default!.ID);
        values.Put(EventsColumns.InterfaceConsts.EventTimezone, timezone);
        return values;
    }

    static void ReplaceReminders(long eventId, List<EventReminder> reminders)
    {
        Resolver.Delete(RemindersColumns.ContentUri!, RemindersColumns.InterfaceConsts.EventId + " = ?", [eventId.ToString()]);
        WriteReminders(eventId, reminders);
    }

    static void ReplaceAttendees(long eventId, List<EventAttendee> attendees)
    {
        // rewriting attendees makes sync adapters send fresh invitations, so leave an unchanged list alone
        var current = new List<(long EventId, CalendarEvent Event)> { (eventId, new CalendarEvent()) };
        var byId = current.ToLookup(x => x.EventId, x => x.Event);
        LoadAttendees(byId, [eventId]);
        var existing = current[0].Event.Attendees;
        if (AttendeeKeys(existing).SequenceEqual(AttendeeKeys(attendees)))
            return;

        Resolver.Delete(AttendeesColumns.ContentUri!, AttendeesColumns.InterfaceConsts.EventId + " = ?", [eventId.ToString()]);
        WriteAttendees(eventId, attendees);
    }

    static IEnumerable<string> AttendeeKeys(IEnumerable<EventAttendee> attendees)
        => attendees
            .Select(a => $"{a.Email?.ToLowerInvariant()}|{a.Name}|{a.Role}|{a.Status}|{a.IsOrganizer}")
            .Order(StringComparer.Ordinal);

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
            values.Put(AttendeesColumns.InterfaceConsts.AttendeeStatus, ToAndroidAttendeeStatus(attendee.Status));
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

    static int ToAndroidAttendeeStatus(AttendeeStatus status) => status switch
    {
        AttendeeStatus.Accepted => (int)CalendarAttendeesStatus.Accepted,
        AttendeeStatus.Declined => (int)CalendarAttendeesStatus.Declined,
        AttendeeStatus.Pending => (int)CalendarAttendeesStatus.Invited,
        AttendeeStatus.Tentative => (int)CalendarAttendeesStatus.Tentative,
        _ => (int)CalendarAttendeesStatus.None
    };
}
