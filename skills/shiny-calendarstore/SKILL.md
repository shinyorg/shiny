---
name: shiny-calendarstore
description: Generate code using Shiny.Calendar for cross-platform device calendar & event access with CRUD, LINQ queries, and Shiny.Core permissions
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
  - calendar LINQ
  - EventKit
  - CalendarContract
  - AppointmentStore
  - calendar AI tools
  - Shiny.Calendar.Extensions.AI
  - AddCalendarAITools
  - CalendarAITools
  - CalendarAICapabilities
---

# Shiny.Calendar Skill

You are an expert in Shiny.Calendar, a cross-platform library for accessing device calendars and
events on iOS, Mac Catalyst, macOS, Android, and Windows.

## When to Use This Skill

Invoke this skill when the user wants to:
- Access device calendars and events (read, create, update, delete)
- Query events with LINQ
- Request calendar permissions using Shiny's AccessState model
- Register the calendar store in DI
- Work with calendar models (events, attendees, reminders)

## Library Overview

**GitHub**: https://github.com/shinyorg/shiny
**NuGet**: `Shiny.Calendar`
**Namespace**: `Shiny.Calendar`

Shiny.Calendar provides:
- Full CRUD operations on device calendars and events
- LINQ query support with native fetch translation (calendar id + start/end window are pushed to the
  native query; other predicates run in-memory)
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
    IQueryable<CalendarEvent> Query();
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

### Query with LINQ

`Query()` (over events) pushes `CalendarId` equality and the `Start`/`End` date window down to the
native fetch; the full predicate is always re-applied in-memory, so any `Where` is safe.

```csharp
// Events on a calendar within a date window
var events = store.Query()
    .Where(e => e.CalendarId == calId
             && e.Start >= DateTimeOffset.Now
             && e.End <= DateTimeOffset.Now.AddDays(7))
    .ToList();

// Free-text (runs in-memory)
var standups = store.Query()
    .Where(e => e.Title.Contains("Standup"))
    .ToList();

// Attendee filter (in-memory) + paging
var withAlice = store.Query()
    .Where(e => e.Attendees.Any(a => a.Email == "alice@example.com"))
    .Skip(0).Take(20)
    .ToList();
```

**Native fetch hints:** `CalendarId` (`==`), `Start` / `End` (`>=`, `<=`). Everything else is in-memory.

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
2. **Prefer `Query().Where(...)` with a date window** — the `Start`/`End`/`CalendarId` hints narrow the native fetch; unbounded reads default to a ~4-month window.
3. **Handle `Restricted`** — iOS write-only and Android partial grants surface as `AccessState.Restricted`.
4. **Don't rely on attendee writes on Apple** — set attendees where supported (Android), and use `ResolveContact` to map an attendee's email back to a device contact.
5. **Use primary constructors** — inject `ICalendarStore` via primary constructor.

## AI Tool Integration (Shiny.Calendar.Extensions.AI)

The optional `Shiny.Calendar.Extensions.AI` package exposes `ICalendarStore` as
`Microsoft.Extensions.AI` tool functions (`AIFunction`s) for LLM agents. You opt-in **per operation**
(read / create / update / delete) — an allow-list you control on behalf of the agent (**not** an OS
permission prompt; the platform calendar permission must already be granted). AOT-compatible
(hand-built schemas, `JsonNode` results — no reflection).

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

Key types:
- `AddCalendarAITools(Action<ICalendarAIToolBuilder>)` — DI extension; throws if nothing is added.
- `ICalendarAIToolBuilder` — `AddCalendar(CalendarAICapabilities)`.
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
