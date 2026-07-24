using Windows.ApplicationModel.Appointments;
using Shiny.Calendar.Internals;
using WinColor = Windows.UI.Color;

namespace Shiny.Calendar;

/// <summary>
/// Windows calendar store backed by <see cref="AppointmentStore"/>.
///
/// Platform limits (see plan/readme): the WinRT appointments API can enumerate all calendars and
/// appointments read-only, but create/update/delete is only permitted inside an app-owned
/// <see cref="AppointmentCalendar"/>. Writes targeting a system calendar throw
/// <see cref="NotSupportedException"/>. Requires the <c>appointments</c> capability in the app manifest.
/// </summary>
public class CalendarStoreImpl : ICalendarStore
{
    const string DefaultAppCalendarName = "Shiny";

    AppointmentStore? readStore;
    AppointmentStore? writeStore;

    async Task<AppointmentStore> GetReadStore()
        => this.readStore ??= await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AllCalendarsReadOnly);

    async Task<AppointmentStore> GetWriteStore()
        => this.writeStore ??= await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);

    public AccessState GetCurrentAccess()
        // The WinRT store cannot be probed without potentially prompting; RequestAccess resolves it.
        => AccessState.Unknown;

    public async Task<AccessState> RequestAccess(CalendarAccessType accessType = CalendarAccessType.ReadWrite, CancellationToken ct = default)
    {
        try
        {
            if (accessType != CalendarAccessType.WriteOnly)
                await this.GetReadStore();
            if (accessType != CalendarAccessType.ReadOnly)
                await this.GetWriteStore();
            return AccessState.Available;
        }
        catch (UnauthorizedAccessException)
        {
            return AccessState.Denied;
        }
    }

    // ── Calendars ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Calendar>> GetAll(CancellationToken ct = default)
    {
        var store = await this.GetReadStore();
        var cals = await store.FindAppointmentCalendarsAsync(FindAppointmentCalendarsOptions.IncludeHidden);
        return cals.Select(ToCalendar).ToList();
    }

    public async Task<Calendar?> GetById(string calendarId, CancellationToken ct = default)
    {
        var store = await this.GetReadStore();
        try
        {
            var cal = await store.GetAppointmentCalendarAsync(calendarId);
            return cal == null ? null : ToCalendar(cal);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> Create(string name, string? color = null, CancellationToken ct = default)
    {
        var store = await this.GetWriteStore();
        var cal = await store.CreateAppointmentCalendarAsync(name);
        var c = ParseColor(color);
        if (c.HasValue)
        {
            cal.DisplayColor = c.Value;
            await cal.SaveAsync();
        }
        return cal.LocalId;
    }

    public async Task Update(string calendarId, string newName, string? newColor = null, CancellationToken ct = default)
    {
        var store = await this.GetWriteStore();
        var cal = await store.GetAppointmentCalendarAsync(calendarId)
            ?? throw new NotSupportedException($"Calendar '{calendarId}' is not an app-owned calendar and cannot be modified on Windows.");

        cal.DisplayName = newName;
        var c = ParseColor(newColor);
        if (c.HasValue)
            cal.DisplayColor = c.Value;
        await cal.SaveAsync();
    }

    public async Task Delete(string calendarId, CancellationToken ct = default)
    {
        var store = await this.GetWriteStore();
        var cal = await store.GetAppointmentCalendarAsync(calendarId)
            ?? throw new NotSupportedException($"Calendar '{calendarId}' is not an app-owned calendar and cannot be deleted on Windows.");
        await cal.DeleteAsync();
    }

    // ── Events ───────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CalendarEvent>> GetEvents(
        string? calendarId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken ct = default
    )
    {
        var store = await this.GetReadStore();
        var (from, to) = ResolveWindow(start, end);

        var options = new FindAppointmentsOptions();
        options.FetchProperties.Add(AppointmentProperties.Subject);
        options.FetchProperties.Add(AppointmentProperties.Details);
        options.FetchProperties.Add(AppointmentProperties.Location);
        options.FetchProperties.Add(AppointmentProperties.StartTime);
        options.FetchProperties.Add(AppointmentProperties.Duration);
        options.FetchProperties.Add(AppointmentProperties.AllDay);
        options.FetchProperties.Add(AppointmentProperties.Reminder);
        options.FetchProperties.Add(AppointmentProperties.BusyStatus);
        options.FetchProperties.Add(AppointmentProperties.Invitees);
        options.FetchProperties.Add(AppointmentProperties.Organizer);
        options.FetchProperties.Add(AppointmentProperties.Recurrence);
        options.FetchProperties.Add(AppointmentProperties.Uri);
        if (!string.IsNullOrWhiteSpace(calendarId))
            options.CalendarIds.Add(calendarId);

        var appts = await store.FindAppointmentsAsync(from, to - from, options);
        return appts.Select(ToEvent).ToList();
    }

    public async Task<CalendarEvent?> GetEvent(string eventId, CancellationToken ct = default)
    {
        var store = await this.GetReadStore();
        try
        {
            var appt = await store.GetAppointmentAsync(eventId);
            return appt == null ? null : ToEvent(appt);
        }
        catch
        {
            return null;
        }
    }

    public IQueryable<CalendarEvent> Query()
        => new CalendarEventQueryable(new CalendarEventQueryProvider(this.ExecuteQuery));

    IEnumerable<CalendarEvent> ExecuteQuery(CalendarEventQueryDescriptor descriptor)
        => this.GetEvents(descriptor.CalendarId, descriptor.StartAfter, descriptor.EndBefore)
            .GetAwaiter()
            .GetResult();

    public async Task<string> CreateEvent(CalendarEvent calendarEvent, CancellationToken ct = default)
    {
        var cal = await this.ResolveWritableCalendar(calendarEvent.CalendarId);
        var appt = new Appointment();
        Populate(appt, calendarEvent);
        await cal.SaveAppointmentAsync(appt);
        return appt.LocalId;
    }

    public async Task UpdateEvent(CalendarEvent calendarEvent, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(calendarEvent.Id))
            throw new ArgumentException("Event Id is required for update.", nameof(calendarEvent));

        var cal = await this.ResolveWritableCalendar(calendarEvent.CalendarId);
        var appt = await cal.GetAppointmentAsync(calendarEvent.Id)
            ?? throw new NotSupportedException("Only events in an app-owned calendar can be updated on Windows.");

        Populate(appt, calendarEvent);
        await cal.SaveAppointmentAsync(appt);
    }

    public async Task DeleteEvent(string eventId, CancellationToken ct = default)
    {
        var store = await this.GetWriteStore();
        var appCals = await store.FindAppointmentCalendarsAsync();
        foreach (var cal in appCals)
        {
            var appt = await cal.GetAppointmentAsync(eventId);
            if (appt != null)
            {
                await cal.DeleteAppointmentAsync(eventId);
                return;
            }
        }
        throw new NotSupportedException("Only events in an app-owned calendar can be deleted on Windows.");
    }

    // ── Mapping ──────────────────────────────────────────────────────

    async Task<AppointmentCalendar> ResolveWritableCalendar(string? calendarId)
    {
        var store = await this.GetWriteStore();
        var appCals = await store.FindAppointmentCalendarsAsync();

        if (!string.IsNullOrWhiteSpace(calendarId))
        {
            var match = appCals.FirstOrDefault(c => c.LocalId == calendarId)
                ?? throw new NotSupportedException($"Calendar '{calendarId}' is not app-owned; Windows only allows writes to app-owned calendars.");
            return match;
        }

        return appCals.FirstOrDefault()
            ?? await store.CreateAppointmentCalendarAsync(DefaultAppCalendarName);
    }

    static void Populate(Appointment appt, CalendarEvent src)
    {
        appt.Subject = src.Title ?? string.Empty;
        appt.Details = src.Description ?? string.Empty;
        appt.Location = src.Location ?? string.Empty;
        appt.StartTime = src.Start;
        appt.Duration = src.End - src.Start;
        appt.AllDay = src.IsAllDay;
        appt.BusyStatus = ToBusyStatus(src.Availability);
        appt.Uri = string.IsNullOrWhiteSpace(src.Url) ? null : new Uri(src.Url);

        // Windows appointments support a single reminder; use the earliest.
        appt.Reminder = src.Reminders.Count > 0
            ? src.Reminders.Min(r => r.Offset)
            : null;

        appt.Invitees.Clear();
        foreach (var attendee in src.Attendees)
        {
            appt.Invitees.Add(new AppointmentInvitee
            {
                DisplayName = attendee.Name ?? string.Empty,
                Address = attendee.Email ?? string.Empty,
                Role = ToInviteeRole(attendee.Role)
            });
        }
    }

    static Calendar ToCalendar(AppointmentCalendar cal) => new()
    {
        Id = cal.LocalId,
        Name = cal.DisplayName,
        Color = ToHex(cal.DisplayColor),
        IsReadOnly = cal.OtherAppWriteAccess == AppointmentCalendarOtherAppWriteAccess.None,
        Account = cal.SourceDisplayName
    };

    static CalendarEvent ToEvent(Appointment appt)
    {
        var evt = new CalendarEvent
        {
            Id = appt.LocalId,
            CalendarId = appt.CalendarId,
            Title = appt.Subject ?? string.Empty,
            Description = appt.Details,
            Location = appt.Location,
            Start = appt.StartTime,
            End = appt.StartTime + appt.Duration,
            IsAllDay = appt.AllDay,
            Availability = FromBusyStatus(appt.BusyStatus),
            Url = appt.Uri?.ToString(),
            IsRecurring = appt.Recurrence != null,
            RecurrenceRule = appt.Recurrence?.Unit.ToString()
        };

        if (appt.Reminder != null)
            evt.Reminders.Add(new EventReminder(appt.Reminder.Value));

        foreach (var invitee in appt.Invitees)
        {
            evt.Attendees.Add(new EventAttendee
            {
                Name = invitee.DisplayName,
                Email = invitee.Address,
                Role = FromInviteeRole(invitee.Role),
                Status = FromInviteeResponse(invitee.Response)
            });
        }

        if (appt.Organizer != null)
        {
            evt.Organizer = new EventAttendee
            {
                Name = appt.Organizer.DisplayName,
                Email = appt.Organizer.Address,
                IsOrganizer = true
            };
        }

        return evt;
    }

    // ── Enum + colour helpers ────────────────────────────────────────

    static AppointmentBusyStatus ToBusyStatus(EventAvailability a) => a switch
    {
        EventAvailability.Free => AppointmentBusyStatus.Free,
        EventAvailability.Tentative => AppointmentBusyStatus.Tentative,
        EventAvailability.Unavailable => AppointmentBusyStatus.OutOfOffice,
        _ => AppointmentBusyStatus.Busy
    };

    static EventAvailability FromBusyStatus(AppointmentBusyStatus s) => s switch
    {
        AppointmentBusyStatus.Free => EventAvailability.Free,
        AppointmentBusyStatus.Tentative => EventAvailability.Tentative,
        AppointmentBusyStatus.OutOfOffice => EventAvailability.Unavailable,
        _ => EventAvailability.Busy
    };

    static AppointmentParticipantRole ToInviteeRole(AttendeeRole role) => role switch
    {
        AttendeeRole.Optional => AppointmentParticipantRole.OptionalAttendee,
        AttendeeRole.Resource => AppointmentParticipantRole.Resource,
        _ => AppointmentParticipantRole.RequiredAttendee
    };

    static AttendeeRole FromInviteeRole(AppointmentParticipantRole role) => role switch
    {
        AppointmentParticipantRole.OptionalAttendee => AttendeeRole.Optional,
        AppointmentParticipantRole.Resource => AttendeeRole.Resource,
        _ => AttendeeRole.Required
    };

    static AttendeeStatus FromInviteeResponse(AppointmentParticipantResponse response) => response switch
    {
        AppointmentParticipantResponse.Accepted => AttendeeStatus.Accepted,
        AppointmentParticipantResponse.Declined => AttendeeStatus.Declined,
        AppointmentParticipantResponse.Tentative => AttendeeStatus.Tentative,
        AppointmentParticipantResponse.NoResponseReceived => AttendeeStatus.Pending,
        _ => AttendeeStatus.Unknown
    };

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

    static WinColor? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;
        var s = hex.TrimStart('#');
        if (s.Length != 6 || !int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
            return null;
        return new WinColor
        {
            A = 255,
            R = (byte)((rgb >> 16) & 0xFF),
            G = (byte)((rgb >> 8) & 0xFF),
            B = (byte)(rgb & 0xFF)
        };
    }

    static string ToHex(WinColor c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
}
