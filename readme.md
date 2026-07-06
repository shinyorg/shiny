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
* **BluetoothLE Client** - scan, connect, GATT, and L2CAP CoC on iOS/macOS, Android, Windows, Linux (BlueZ), and Blazor WebAssembly (Web Bluetooth)
* **BluetoothLE Hosting** - GATT server, advertising, iBeacon broadcasting, and L2CAP CoC listeners on iOS/macOS, Android, Windows, and Linux (BlueZ AF_BLUETOOTH sockets)
* **Locations** - foreground/background GPS, geofence monitoring, and motion-activity recognition (CMMotionActivity / ActivityRecognition)
* **Contacts** - cross-platform device contact access with CRUD and LINQ queries (iOS/Android)
* **Local Notifications** - scheduled, repeating, and geofence-triggered notifications on iOS/macOS, Android, Windows, and Linux (`org.freedesktop.Notifications` D-Bus)
* **Push Notifications** - native APNs/FCM, Firebase Cloud Messaging, Azure Notification Hubs, and Blazor (Web Push)
* **Core** - hosting, DI, key/value stores, object-store binding, lifecycle hooks, connectivity & battery monitoring, and the platform abstractions every Shiny module builds on

## AI Tools

Optional `*.Extensions.AI` packages expose Shiny modules as [`Microsoft.Extensions.AI`](https://learn.microsoft.com/dotnet/ai/) tool functions (`AIFunction`s) for LLM agents. You opt-in exactly which operations the model can see - a read/write allow-list you control on behalf of the agent (this is *not* an OS permission prompt; the underlying platform permissions must already be granted). Resolve the generated `*AITools` bundle from DI and pass `.Tools` to any `IChatClient`. All are AOT-compatible (hand-built schemas, no reflection).

* **Shiny.Contacts.Extensions.AI** - `AddContactsAITools(...)` → `search_contacts`, `get_contact`, and (write) `create_contact`, `update_contact`, `delete_contact`
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
