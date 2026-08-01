---
name: shiny-calendarstore
description: Generate code using Shiny.Calendar for cross-platform device calendar & event access with CRUD, a fluent async query builder, and Shiny.Core permissions
auto_invoke: true
triggers:
  - calendar store
  - calendar
  - calendars
  - calendar event
  - ICalendarStore
  - CalendarStore
  - AddCalendarStore
  - Shiny.Calendar
  - device calendar
  - read calendar
  - write calendar
  - create event
  - update event
  - delete event
  - calendar query
  - CalendarEventQuery
  - CalendarEventSortField
  - search events
  - EventKit
  - CalendarContract
  - AppointmentStore
  - calendar AI tools
  - Shiny.Calendar.Extensions.AI
  - AddCalendarAITools
  - CalendarAITools
  - CalendarAICapabilities
  - ICalendarAIToolBuilder
  - AddCalendars
  - calendar AI filter
  - per-calendar AI access
---

# Shiny.Calendar Skill

You are an expert in Shiny.Calendar, a cross-platform library for accessing device calendars and
events on iOS, Mac Catalyst, macOS, Android, and Windows.

## When to Use This Skill

Invoke this skill when the user wants to:
- Access device calendars and events (read, create, update, delete)
- Query events (filter/sort/page over a date window)
- Request calendar permissions using Shiny's AccessState model
- Register the calendar store in DI
- Work with calendar models (events, attendees, reminders)

## Library Overview

**GitHub**: https://github.com/shinyorg/shiny
**NuGet**: `Shiny.Calendar`
**Namespace**: `Shiny.Calendar`

Shiny.Calendar provides:
- Full CRUD operations on device calendars and events
- A fluent async query builder with native fetch translation (calendar id + start/end window are
  pushed to the native query; other filters, sorting and paging run in-memory)
- Permission handling via Shiny.Core's `AccessState` model
- Dependency injection integration
- AOT and trimmer compatible

**Platform backends:** EventKit (iOS/Mac Catalyst/macOS), CalendarContract (Android),
`Windows.ApplicationModel.Appointments.AppointmentStore` (Windows).

## Setup

### 1. Install NuGet Package
```bash
dotnet add package Shiny.Calendar
```

### 2. Register in MauiProgram.cs
The app must call `.UseShiny()` (from Shiny.Hosting.Maui) so platform services like permissions are wired up.
```csharp
using Shiny;

builder.UseShiny();
builder.Services.AddCalendarStore();
```

### 3. Platform Permissions

**Android** — Add to `AndroidManifest.xml`:
```xml
<uses-permission android:name="android.permission.READ_CALENDAR" />
<uses-permission android:name="android.permission.WRITE_CALENDAR" />
```

**iOS 17+ / Mac Catalyst / macOS** — Add to `Info.plist`:
```xml
<key>NSCalendarsFullAccessUsageDescription</key>
<string>This app needs access to your calendar.</string>
<!-- If only adding events, you can request write-only access instead: -->
<key>NSCalendarsWriteOnlyAccessUsageDescription</key>
<string>This app needs to add events to your calendar.</string>
```
**iOS < 17** — Add `NSCalendarsUsageDescription`.

**Mac Catalyst / sandboxed macOS** — the `Info.plist` keys are not enough. The App Sandbox (which
Mac Catalyst enables by default) also requires the calendar entitlement, or `RequestAccess` returns
`Denied` with no prompt ever appearing:
```xml
<CustomEntitlements Include="com.apple.security.personal-information.calendars"
                    Type="Boolean" Value="true" />
```

**Windows** — Add to `Package.appxmanifest`:
```xml
<uap:Capability Name="appointments" />
```

## Permissions

Permissions use Shiny.Core's `Shiny.AccessState` model. `ICalendarStore` exposes:

```csharp
// Request access (triggers OS prompt if needed). Pass the access level you need.
var access = await calendarStore.RequestAccess(CalendarAccessType.ReadWrite);
if (access != AccessState.Available)
    return; // handle denied / restricted

// Check current state without prompting
var current = calendarStore.GetCurrentAccess();
```

`CalendarAccessType`: `ReadOnly`, `WriteOnly` (iOS 17+ add-only), `ReadWrite` (default).

- `AccessState.Available` — access granted
- `AccessState.Restricted` — partial (e.g. iOS write-only, or Android read-only/write-only)
- `AccessState.Denied` — denied
- `AccessState.Unknown` — not yet determined (also the Windows `GetCurrentAccess()` result — call `RequestAccess`)

## API Reference

### ICalendarStore Interface

```csharp
public interface ICalendarStore
{
    AccessState GetCurrentAccess();
    Task<AccessState> RequestAccess(CalendarAccessType accessType = CalendarAccessType.ReadWrite, CancellationToken ct = default);

    // Calendars (the entity is implied by the interface name, so no "Calendar" suffix)
    Task<IReadOnlyList<Calendar>> GetAll(CancellationToken ct = default);
    Task<Calendar?> GetById(string calendarId, CancellationToken ct = default);
    Task<string> Create(string name, string? color = null, CancellationToken ct = default);
    Task Update(string calendarId, string newName, string? newColor = null, CancellationToken ct = default);
    Task Delete(string calendarId, CancellationToken ct = default);

    // Events
    Task<IReadOnlyList<CalendarEvent>> GetEvents(string? calendarId = null, DateTimeOffset? start = null, DateTimeOffset? end = null, CancellationToken ct = default);
    Task<CalendarEvent?> GetEvent(string eventId, CancellationToken ct = default);
    CalendarEventQuery Query();
    Task<string> CreateEvent(CalendarEvent calendarEvent, CancellationToken ct = default);
    Task UpdateEvent(CalendarEvent calendarEvent, CancellationToken ct = default);
    Task DeleteEvent(string eventId, bool deleteSeries = false, CancellationToken ct = default);
}
```

### Convenience Extension Methods

```csharp
Task<string> store.CreateEvent(string? calendarId, string title, string? description, string? location,
    DateTimeOffset start, DateTimeOffset end, bool isAllDay = false,
    IEnumerable<EventReminder>? reminders = null, CancellationToken ct = default);

Task<string> store.CreateAllDayEvent(string? calendarId, string title, string? description, string? location,
    DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken ct = default);

// Bridge to Shiny.Contacts — resolve an attendee to a device contact by email
Task<Shiny.Contacts.Contact?> attendee.ResolveContact(IContactStore contactStore, CancellationToken ct = default);
```

### Querying events

`Query()` returns a **`CalendarEventQuery`** builder. It is lazy — nothing runs until `ToListAsync` /
`FirstOrDefaultAsync` / `CountAsync`, and the native calendar read happens off the calling thread, so
awaiting it from a UI/view-model method is correct (do NOT wrap it in `Task.Run`). There is **no
`IQueryable`** — do not write `.Where(e => …).ToList()` LINQ against `Query()`.

```csharp
public sealed class CalendarEventQuery
{
    CalendarEventQuery ForCalendar(string? calendarId);   // native hint
    CalendarEventQuery From(DateTimeOffset start);        // native hint
    CalendarEventQuery To(DateTimeOffset end);            // native hint
    CalendarEventQuery Between(DateTimeOffset start, DateTimeOffset end);
    CalendarEventQuery Where(Func<CalendarEvent, bool> predicate);   // in-memory, ANDed
    CalendarEventQuery TitleContains(string text);                   // in-memory, case-insensitive
    CalendarEventQuery OrderBy(CalendarEventSortField field, bool descending = false);
    CalendarEventQuery ThenBy(CalendarEventSortField field, bool descending = false);
    CalendarEventQuery Skip(int count);
    CalendarEventQuery Take(int count);

    Task<IReadOnlyList<CalendarEvent>> ToListAsync(CancellationToken ct = default);
    Task<CalendarEvent?> FirstOrDefaultAsync(CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
```

```csharp
// Events on a calendar within a date window (both hints pushed to the native fetch)
var events = await store.Query()
    .ForCalendar(calId)
    .Between(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(7))
    .OrderBy(CalendarEventSortField.Start)
    .ToListAsync(ct);

// Free-text title (in-memory)
var standups = await store.Query()
    .From(DateTimeOffset.Now.AddDays(-30))
    .TitleContains("standup")
    .ToListAsync(ct);

// Attendee filter (in-memory) + paging
var withAlice = await store.Query()
    .Where(e => e.Attendees.Any(a => a.Email == "alice@example.com"))
    .Skip(0).Take(20)
    .ToListAsync(ct);

// Count
var busy = await store.Query()
    .Between(from, to)
    .Where(e => e.Availability == EventAvailability.Busy)
    .CountAsync(ct);
```

**Native fetch hints:** `ForCalendar`, `From`, `To`, `Between`. Everything else — `Where`,
`TitleContains`, sorting, `Skip`/`Take` — is applied to the fetched events.

**`CalendarEventSortField`:** `Start`, `End`, `Title`. `ThenBy` throws without a preceding `OrderBy`.

### Create an Event

```csharp
var evt = new CalendarEvent("Team Sync", DateTimeOffset.Now.AddHours(1), DateTimeOffset.Now.AddHours(2))
{
    CalendarId = calId,           // optional; default calendar is used if null
    Location = "Room 3",
    Description = "Weekly sync"
};
evt.Reminders.Add(new EventReminder(TimeSpan.FromMinutes(15)));   // 15 min before
evt.Attendees.Add(new EventAttendee("Alice", "alice@example.com"));

string id = await store.CreateEvent(evt);

// or the convenience overload
string id2 = await store.CreateEvent(calId, "Lunch", null, "Cafe",
    DateTimeOffset.Now.AddHours(4), DateTimeOffset.Now.AddHours(5));
```

### Update / Delete an Event

```csharp
var evt = await store.GetEvent(id);
evt.Location = "Room 5";
await store.UpdateEvent(evt);

// Deletes only this occurrence when the event recurs.
await store.DeleteEvent(id);
```

#### Deleting a recurring event

`deleteSeries` decides whether a recurring event loses one occurrence or the rest of the series. It
is ignored for non-recurring events, so it is always safe to pass. **Never guess on a recurring
event — prompt the user**, keyed off `CalendarEvent.IsRecurring`:

```csharp
if (evt.IsRecurring)
{
    var choice = await dialogs.ActionSheet(
        $"Delete \"{evt.Title}\"?", "Cancel", "Delete All Future Events", "Delete This Event");

    if (choice is not ("Delete This Event" or "Delete All Future Events"))
        return;

    await store.DeleteEvent(evt.Id!, choice == "Delete All Future Events");
}
else
{
    await store.DeleteEvent(evt.Id!);
}
```

Platform behaviour:

| Platform | `deleteSeries: false` | `deleteSeries: true` |
|---|---|---|
| iOS / Mac Catalyst / macOS | `EKSpan.ThisEvent` | `EKSpan.FutureEvents` |
| Android | Inserts a cancellation exception for the occurrence | Deletes the `Events` row (whole series) |
| Windows | Deletes the appointment — no per-instance delete exists in `AppointmentStore`, so the flag has no effect | Same |

Android reads series masters from the `Events` table rather than expanded instances, so
`deleteSeries: false` cancels the series' own `DTSTART` — i.e. the first occurrence.

## Models

### Calendar
| Property   | Type      |
|------------|-----------|
| Id         | `string?` |
| Name       | `string`  |
| Color      | `string?` (hex, e.g. `#FF3B30`) |
| IsReadOnly | `bool`    |
| Account    | `string?` |

### CalendarEvent
| Property        | Type                    |
|-----------------|-------------------------|
| Id              | `string?`               |
| CalendarId      | `string?`               |
| Title           | `string`                |
| Description     | `string?`               |
| Location        | `string?`               |
| Start / End     | `DateTimeOffset`        |
| IsAllDay        | `bool`                  |
| Availability    | `EventAvailability`     |
| Url             | `string?`               |
| IsRecurring     | `bool` (read-only)      |
| RecurrenceRule  | `string?` (read-only)   |
| Reminders       | `List<EventReminder>`   |
| Attendees       | `List<EventAttendee>`   |
| Organizer       | `EventAttendee?` (read-only) |

### EventReminder
`Offset` (`TimeSpan`) — how far before the start the reminder fires.

### EventAttendee
`Name`, `Email`, `Role` (`AttendeeRole`), `Status` (`AttendeeStatus`), `IsOrganizer`.

### Enums
- **EventAvailability:** Busy, Free, Tentative, Unavailable
- **AttendeeRole:** Required, Optional, Resource, Unknown
- **AttendeeStatus:** Unknown, Pending, Accepted, Declined, Tentative
- **CalendarAccessType:** ReadOnly, WriteOnly, ReadWrite

## Platform Notes & Caveats

1. **Recurrence is read-only** on all platforms (`IsRecurring` / `RecurrenceRule` are surfaced but not written).
2. **Apple (EventKit):** attendees **cannot be written** — `Attendees` you set on create/update are ignored. Reminders use a relative lead-time.
3. **Android (CalendarContract):** event CRUD works with permissions. Creating/modifying **calendars** goes through sync-adapter semantics and may behave differently across OEMs.
4. **Windows (AppointmentStore):** best-effort. Reads/queries cover all calendars; **create/update/delete only work inside an app-owned calendar** — writes targeting a system calendar throw `NotSupportedException`. Requires the `appointments` capability.

## Best Practices

1. **Always request access first** — `await store.RequestAccess(...)` and check `AccessState.Available`.
2. **Always set a date window** — `Between(from, to)` (or `From`/`To`) plus `ForCalendar(id)` are the only hints pushed to the native fetch; unbounded reads default to a ~4-month window.
3. **Await the query, don't wrap it** — `ToListAsync` already does the native read off the calling thread. Do not add `Task.Run` around it.
4. **Handle `Restricted`** — iOS write-only and Android partial grants surface as `AccessState.Restricted`.
5. **Don't rely on attendee writes on Apple** — set attendees where supported (Android), and use `ResolveContact` to map an attendee's email back to a device contact.
6. **Use primary constructors** — inject `ICalendarStore` via primary constructor.

## AI Tool Integration (Shiny.Calendar.Extensions.AI)

The optional `Shiny.Calendar.Extensions.AI` package exposes `ICalendarStore` as
`Microsoft.Extensions.AI` tool functions (`AIFunction`s) for LLM agents. You opt-in **per operation**
(read / create / update / delete) and, optionally, **per calendar id** — an allow-list you control on
behalf of the agent (**not** an OS permission prompt; the platform calendar permission must already be
granted). AOT-compatible (hand-built schemas, `JsonNode` results — no reflection).

```csharp
using Shiny.Calendar;
using Shiny.Calendar.Extensions.AI;

builder.Services.AddCalendarStore();                              // registers ICalendarStore
builder.Services.AddCalendarAITools(tools => tools
    .AddCalendar(CalendarAICapabilities.Read | CalendarAICapabilities.Create)  // read + create only
);

// resolve the bundle and pass the tools to any IChatClient
var tools = sp.GetRequiredService<CalendarAITools>().Tools;
var response = await chatClient.GetResponseAsync(
    messages,
    new ChatOptions { Tools = [.. tools] }
);
```

### Scoping to specific calendars

`AddCalendar(calendarId, capabilities)` grants capabilities for **one calendar**, and the entry
**replaces** the global set for that calendar — so it can widen *or* narrow access. Use it to give the
agent read-only access to everything but read/write on a work calendar, to expose an allow-list of
calendars and nothing else, or to hide one calendar entirely with `None`.

```csharp
builder.Services.AddCalendarAITools(tools => tools
    .AddCalendar(CalendarAICapabilities.Read)                    // global default: read everything
    .AddCalendar(workCalendarId, CalendarAICapabilities.All)     // …but full read/write here
    .AddCalendar(privateCalendarId, CalendarAICapabilities.None) // …and hide this one entirely
);

// strict allow-list — no global grant, so ONLY these two calendars exist to the agent
builder.Services.AddCalendarAITools(tools => tools
    .AddCalendar(workCalendarId, CalendarAICapabilities.All)
    .AddCalendars([teamCalendarId, holidayCalendarId], CalendarAICapabilities.Read)
);
```

Enforcement (the tools do this on every call — the model can't work around it):
- `list_calendars` returns only calendars with at least one capability, each with an
  `allowedOperations` array (`read`/`create`/`update`/`delete`) so the model knows where it may write.
- `search_events` searches only readable calendars; an explicit `calendarId` outside the filter errors.
- `get_event` / `update_event` / `delete_event` resolve the event's calendar first and refuse when it
  isn't allowed.
- `create_event` refuses a disallowed `calendarId`. With no global `Create` grant, omitting
  `calendarId` errors too — the device default calendar can't be vetted ahead of time, so the model is
  told to pick one from `list_calendars`.
- A tool is generated when **any** calendar grants that capability, so
  `AddCalendar("work", All)` alone still produces `create_event`/`update_event`/`delete_event`.

Calendar ids are platform-assigned, so they're normally chosen by the user at runtime and persisted.
Use the service-provider overload to read them back when the tools are first resolved:

```csharp
builder.Services.AddCalendarAITools((sp, tools) =>
{
    var settings = sp.GetRequiredService<AppSettings>();   // ids the user picked, persisted
    tools.AddCalendars(settings.AgentCalendarIds, CalendarAICapabilities.All);
});
```

Key types:
- `AddCalendarAITools(Action<ICalendarAIToolBuilder>)` — DI extension; throws if nothing is added.
- `AddCalendarAITools(Action<IServiceProvider, ICalendarAIToolBuilder>)` — same, but the callback runs on first resolve so the filter can come from your own services.
- `ICalendarAIToolBuilder` — `AddCalendar(CalendarAICapabilities)` (global), `AddCalendar(string calendarId, CalendarAICapabilities)`, `AddCalendars(IEnumerable<string> calendarIds, CalendarAICapabilities)`.
- `CalendarAICapabilities` `[Flags]` — `None`, `Read`, `Create`, `Update`, `Delete`, `Write` (= Create|Update|Delete), `All` (= Read|Write). Combine flags to allow operations independently.
- `CalendarAITools` — resolve from DI; `.Tools` is `IReadOnlyList<AITool>`.

Generated tools (only for opted-in capabilities):
- **Read** → `list_calendars`, `search_events` (date window + free-text), `get_event` (by id)
- **Create** → `create_event`
- **Update** → `update_event`
- **Delete** → `delete_event`

> The AI tools assume permissions are already granted — they do **not** trigger the platform
> permission UI (needs a foreground activity). Call `ICalendarStore.RequestAccess(...)` from the app
> before invoking the agent. `delete_event` is irreversible — instruct the model to confirm with the
> user first.
