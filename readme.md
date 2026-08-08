# Shiny Client for .NET
<img src="https://github.com/shinyorg/shiny/raw/master/art/logo.png" width="100" /> 

Shiny is a cross-platform framework designed to make working with device services and background processes easy, testable, and consistent while bringing
things like dependency injection & logging in a structured way to your code!

## Features
* Handles all of the cruft like Permissions, main thread traversal, persistent storage and app restarts
* Brings your infrastructure to the background
* Provides logging to ensure you know when your services fail in the background
* Gives a clean & testable API surface for your code
* Native AOT & trim-friendly across all modules
* Cross-platform: iOS, Android, Mac Catalyst, macOS, Windows, Linux, and Blazor WebAssembly (where the platform allows)

## Modules
* **Background Jobs** - periodic background work backed by `BGTaskScheduler` (iOS), `WorkManager` (Android), COM-activated background tasks (Windows), and an in-process managed runner for Linux/macOS/Blazor WASM
* **HTTP Transfers** - resumable background uploads/downloads on `NSURLSession` (iOS), an `HttpClient`-driven managed loop with Range-based resume (Android, Windows, Linux, macOS, .NET base), and Service Worker Background Sync (Blazor WASM). Pause/resume support - pause stops a transfer without cancelling it (downloads continue from where they left off, uploads restart). First-class Azure Blob Storage and AWS S3 (SigV4) request builders included
* **Data Sync** - bidirectional JSON record sync over HTTP with the same platform tiers as HTTP Transfers: outbox + inbox on `NSURLSession` (iOS/Mac Catalyst), Foreground Service + HttpClient (Android), `HttpClient` + connectivity loop (Windows/Linux/macOS), and LocalStorage-backed HttpClient (Blazor WASM). Includes batched outbox, tombstones, conflict resolution, retry with exponential backoff, and AOT-safe serialization through `Shiny.Json`
* **Network Discovery** - three local-network discovery protocols in one package, each with browse, resolve, and publish
  * **mDNS/DNS-SD** (Bonjour/Zeroconf) - backed by `NSNetService` on iOS/Mac Catalyst/macOS (so **no** `com.apple.developer.networking.multicast` entitlement is needed), `NsdManager` on Android (no multicast lock), and a dependency-free managed responder on UDP 5353 for Windows, Linux, macOS console, and server .NET. `await foreach (var r in mdns.Browse("_http._tcp", ct))` for live discovery, `BrowseOnce(...)` for a one-shot scan, and `Publish(...)` to advertise your own service with TXT metadata
  * **SSDP/UPnP** - find routers, media servers, Sonos, Roku, and smart TVs. `ssdp.SearchAll()` for a one-shot sweep, `Browse(...)` for a live list keyed on UDN with `ssdp:alive`/`byebye` tracking and BOOTID-aware expiry, `GetDescription(...)` to fetch and parse the device description (friendly name, model, icons, service list), and `Publish(...)` to advertise your own root device. Discovery and description only - no SOAP action invocation, no GENA eventing
  * **WS-Discovery** - find ONVIF cameras, WSD printers/scanners, and Windows machines. Probe/Resolve/Hello/Bye on both the 2005 (ONVIF/Windows) and 2009 (OASIS) profiles, with correct QName-prefix resolution for `Types`, RFC 3986 segment-prefix scope matching, and `ProbeOnvifCameras(...)` as a shortcut
  * **Platform requirements differ by protocol.** mDNS needs no entitlement anywhere. SSDP and WS-Discovery have no OS-level API on any platform, so they use raw multicast: iOS requires the Apple-approved `com.apple.developer.networking.multicast` entitlement, Android needs `CHANGE_WIFI_MULTICAST_STATE` (the multicast lock is acquired for you) plus `ACCESS_LOCAL_NETWORK` from Android 17. Sandboxed hosts still need their usual network permission - `com.apple.security.network.client`/`.server` on Mac Catalyst & macOS, `privateNetworkClientServer` for packaged Windows apps. A missing one throws `DiscoveryPermissionException` naming exactly what to add, rather than silently finding nothing
* **BluetoothLE Client** - scan, connect, GATT, and L2CAP CoC on iOS/macOS, Android, Windows, Linux (BlueZ), and Blazor WebAssembly (Web Bluetooth)
* **BluetoothLE Hosting** - GATT server, advertising, iBeacon broadcasting, and L2CAP CoC listeners on iOS/macOS, Android, Windows, and Linux (BlueZ AF_BLUETOOTH sockets)
* **Locations** - foreground/background GPS, geofence monitoring, and motion-activity recognition (CMMotionActivity / ActivityRecognition)
* **Contacts** - cross-platform device contact access with CRUD and a fluent async query builder (iOS/Android): `await store.Query().Search(text).OrderBy(ContactSortField.FamilyName).ToListAsync(ct)`
* **Calendar** - cross-platform calendar & event access with CRUD and a fluent async query builder on iOS/Mac Catalyst/macOS (EventKit), Android (CalendarContract), and Windows (`AppointmentStore`, best-effort - system events are read-only, writes go to an app-owned calendar). `await store.Query().ForCalendar(id).Between(from, to).OrderBy(CalendarEventSortField.Start).ToListAsync(ct)` - the calendar id and date window are pushed down to the native fetch, which runs off the calling thread. `DeleteEvent(id, deleteSeries)` chooses between removing one occurrence of a recurring event or the rest of the series
* **Local Notifications** - scheduled, repeating, and geofence-triggered notifications on iOS/macOS, Android, Windows, and Linux (`org.freedesktop.Notifications` D-Bus)
* **Push Notifications** - native APNs/FCM, Firebase Cloud Messaging, Azure Notification Hubs, and Blazor (Web Push)
* **Core** - hosting, DI, key/value stores, object-store binding, lifecycle hooks, connectivity & battery monitoring, and the platform abstractions every Shiny module builds on

## AI Tools

Optional `*.Extensions.AI` packages expose Shiny modules as [`Microsoft.Extensions.AI`](https://learn.microsoft.com/dotnet/ai/) tool functions (`AIFunction`s) for LLM agents. You opt-in exactly which operations the model can see - a read/write allow-list you control on behalf of the agent (this is *not* an OS permission prompt; the underlying platform permissions must already be granted). Resolve the generated `*AITools` bundle from DI and pass `.Tools` to any `IChatClient`. All are AOT-compatible (hand-built schemas, no reflection).

* **Shiny.Contacts.Extensions.AI** - `AddContactsAITools(...)` → `search_contacts`, `get_contact`, and (write) `create_contact`, `update_contact`, `delete_contact`
* **Shiny.Calendar.Extensions.AI** - `AddCalendarAITools(...)` → `list_calendars`, `search_events`, `get_event`, and independently opt-in `create_event`, `update_event`, `delete_event` (per-operation `Read`/`Create`/`Update`/`Delete` capability flags). Capabilities apply to every calendar by default, or per calendar id - `AddCalendar(workId, All)` alongside a global `Read` gives the agent read-only access everywhere but read/write on one calendar, and `None` hides a calendar entirely
* **Shiny.Notifications.Extensions.AI** - `AddNotificationAITools(...)` → reminder-framed `list_reminders`, and (write) `create_reminder` (one-time or daily), `cancel_reminder`
* **Shiny.Locations.Extensions.AI** - `AddLocationAITool()` → read-only `get_current_location`, `get_distance_to`, `estimate_travel_time`

```csharp
builder.Services.AddContactStore();
builder.Services.AddContactsAITools(b => b.AddContacts(ContactAICapabilities.ReadWrite));

builder.Services.AddNotifications();
builder.Services.AddNotificationAITools(b => b.AddReminders(ReminderAICapabilities.ReadWrite));

builder.Services.AddGps();
builder.Services.AddLocationAITool();

// later, hand the tools to a chat client
var tools = sp.GetRequiredService<ContactAITools>().Tools;
var response = await chatClient.GetResponseAsync(messages, new ChatOptions { Tools = [.. tools] });
```

## Links
* [Documentation](https://shinylib.net)
* [Change Log](https://shinylib.net/release-notes/client/)
* [Community Support](https://github.com/shinyorg/shiny/discussions)
* [NuGets](https://www.nuget.org/profiles/ShinyLib)
* [AI Coding Skills](https://shinylib.net/foundation/ai-skills/)
