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
* **HTTP Transfers** - resumable background uploads/downloads on `NSURLSession` (iOS), an `HttpClient`-driven managed loop with Range-based resume (Android, Windows, Linux, macOS, .NET base), and Service Worker Background Sync (Blazor WASM). Pause/resume support - pause stops a transfer without cancelling it (downloads continue from where they left off, uploads restart). First-class Azure Blob Storage and AWS S3 (SigV4) request builders included. `AddTransferProgress()` puts progress in front of the user - an iOS Live Activity, or the Android foreground-service notification promoted to an Android 16 live update - from one manager, with no code in your transfer delegate
* **Data Sync** - bidirectional JSON record sync over HTTP with the same platform tiers as HTTP Transfers: outbox + inbox on `NSURLSession` (iOS/Mac Catalyst), Foreground Service + HttpClient (Android), `HttpClient` + connectivity loop (Windows/Linux/macOS), and LocalStorage-backed HttpClient (Blazor WASM). Includes batched outbox, tombstones, conflict resolution, retry with exponential backoff, and AOT-safe serialization through `Shiny.Json`
* **Network Discovery** - three local-network discovery protocols in one package, each with browse, resolve, and publish
  * **mDNS/DNS-SD** (Bonjour/Zeroconf) - backed by `NSNetService` on iOS/Mac Catalyst/macOS (so **no** `com.apple.developer.networking.multicast` entitlement is needed), `NsdManager` on Android (no multicast lock), and a dependency-free managed responder on UDP 5353 for Windows, Linux, macOS console, and server .NET. `await foreach (var r in mdns.Browse("_http._tcp", ct))` for live discovery, `BrowseOnce(...)` for a one-shot scan, and `Publish(...)` to advertise your own service with TXT metadata
  * **SSDP/UPnP** - find routers, media servers, Sonos, Roku, and smart TVs. `ssdp.SearchAll()` for a one-shot sweep, `Browse(...)` for a live list keyed on UDN with `ssdp:alive`/`byebye` tracking and BOOTID-aware expiry, `GetDescription(...)` to fetch and parse the device description (friendly name, model, icons, service list), and `Publish(...)` to advertise your own root device. Discovery and description only - no SOAP action invocation, no GENA eventing
  * **WS-Discovery** - find ONVIF cameras, WSD printers/scanners, and Windows machines. Probe/Resolve/Hello/Bye on both the 2005 (ONVIF/Windows) and 2009 (OASIS) profiles, with correct QName-prefix resolution for `Types`, RFC 3986 segment-prefix scope matching, and `ProbeOnvifCameras(...)` as a shortcut
  * **Platform requirements differ by protocol.** mDNS needs no entitlement anywhere. SSDP and WS-Discovery have no OS-level API on any platform, so they use raw multicast: iOS requires the Apple-approved `com.apple.developer.networking.multicast` entitlement, Android needs `CHANGE_WIFI_MULTICAST_STATE` (the multicast lock is acquired for you) plus `ACCESS_LOCAL_NETWORK` from Android 17. Sandboxed hosts still need their usual network permission - `com.apple.security.network.client`/`.server` on Mac Catalyst & macOS, `privateNetworkClientServer` for packaged Windows apps. A missing one throws `DiscoveryPermissionException` naming exactly what to add, rather than silently finding nothing
* **Wi-Fi** - scan for access points, join and leave networks, manage the networks the device has saved, watch the current network's SSID/signal/IP/DNS, and host a hotspot
  * **Platform reach is genuinely uneven, and the API says so rather than pretending otherwise.** Every manager publishes a `WifiCapabilities` flags property; anything unavailable throws `WifiNotSupportedException` naming the specific limit (an Apple entitlement, an Android API level that revoked the call, a platform with no such concept). Check the flag to branch, catch the exception as a backstop
  * **`IWifiManager`** - `Scan(ct)` returns one `WifiNetwork` per BSSID with SSID, security scheme, dBm + 0-100 signal, frequency, band and channel. `Connect(new WifiConnectionRequest(ssid) { Passphrase = ... })` joins and waits for DHCP rather than returning on association, and with `Remember` left on also persists the network for later (a `WifiNetworkSuggestion` on Android 11+, an ordinary profile elsewhere). `CurrentNetwork` reports the joined network - SSID, BSSID, security, signal, plus every IP, DNS resolver, gateway and mask - and `Changed` fires with the new `WifiNetworkInfo?` (null when Wi-Fi drops), de-duplicated so the chatty native watchers behind it do not leak through
  * **Known networks** - `GetKnownNetworks()` lists what the device has saved as `KnownWifiNetwork` (opaque platform `Id`, SSID, security, hidden), `Forget(id)` deletes one, and `Connect(id)` rejoins one without handing the passphrase over again. The scope differs and the API says which: iOS/Mac Catalyst and Android only ever disclose **your own app's** entries (`NEHotspotConfigurationManager.getConfiguredSSIDs`, network suggestions), while Windows, macOS and Linux hand back every profile on the machine. `Connect(id)` works on Windows, macOS, Linux and Android below API 29 - iOS and modern Android treat a saved network as a standing hint the OS acts on, with no call to force it. The `Id` is the platform's own handle: a NetworkManager connection UUID on Linux, a numeric network id on legacy Android, the SSID everywhere else
  * **`IWifiHotspot`** - `Start(...)` returns an `IHotspotSession` carrying the SSID and passphrase actually in use; dispose it to bring the access point down. `GetClients()` lists joined devices with MAC and address on Windows and Linux. Android raises a *local-only* hotspot (clients reach the device, not the internet) and picks the SSID/passphrase itself, Windows shares the machine's internet connection, Linux runs NetworkManager AP mode with DHCP + NAT, and iOS/macOS have no hotspot API at all
  * **Backends** - `WifiManager` + `ConnectivityManager` on Android (specifier-based joins from API 29, legacy `WifiConfiguration` below), `NEHotspotConfiguration` + CaptiveNetwork on iOS/Mac Catalyst, CoreWLAN on macOS, `WiFiAdapter` + `NetworkOperatorTetheringManager` + `Radio` on Windows, and NetworkManager over D-Bus on Linux (separate `Shiny.Net.Wifi.Linux` package). Saved profiles are outside WinRT entirely, so Windows reaches `wlanapi.dll` directly for those three calls. The plain .NET target still reports IP/DNS off the wireless interface and raises `Changed`; the Wi-Fi-specific calls throw
  * **Permissions** - Android needs `ACCESS_WIFI_STATE`, `CHANGE_WIFI_STATE`, `ACCESS_FINE_LOCATION` and `NEARBY_WIFI_DEVICES` (API 33+); iOS needs the *Hotspot Configuration* and *Access WiFi Information* capabilities plus `NSLocationWhenInUseUsageDescription`; macOS needs the location usage description; Windows needs `wiFiControl` and `radios`; Linux gates the mutating calls behind polkit. A scan that comes back empty because location was refused throws `WifiPermissionException` instead of looking like an empty neighbourhood
* **BluetoothLE Client** - scan, connect, GATT, and L2CAP CoC on iOS/macOS, Android, Windows, Linux (BlueZ), and Blazor WebAssembly (Web Bluetooth). L2CAP file transfers in both directions - `await peripheral.UploadFile(psm, path, onProgress: ...)` / `DownloadFile(...)` - with the same transfer metrics as HTTP Transfers (percent complete, bytes/sec, ETA), plus Rx `*WithProgress` variants
* **BluetoothLE Hosting** - GATT server, advertising, iBeacon broadcasting, and L2CAP CoC listeners on iOS/macOS, Android, Windows, and Linux (BlueZ AF_BLUETOOTH sockets). `OpenL2CapFileServer(rootDirectory)` turns a PSM into a file server that connected centrals can push to and pull from, with size limits, per-request authorization, path-traversal protection, and progress callbacks. A bundled source generator turns `[BleService]` / `[L2CapService]` partial classes into the imperative registrations for you - handler signatures bind by type, GATT status and offset handling is generated, and each connected central gets a partial `{Service}Context` you can stamp your own properties onto (all reflection-free, so it stays AOT-safe)
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
