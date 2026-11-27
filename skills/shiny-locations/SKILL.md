---
name: shiny-locations
description: GPS tracking, geofence monitoring (enter/exit/dwell), reverse geocoding, and motion activity recognition for .NET MAUI, iOS, Android, macOS, Windows, Linux (GeoClue2) and Blazor using Shiny.Gps and Shiny.Geofencing (Shiny.Locations meta-package), plus unlimited GPS-driven region geofencing (Shiny.Geofencing.Regions) over SQLite documents or the reverse geocoder
auto_invoke: true
triggers:
  - gps
  - geofence
  - geofencing
  - location
  - position
  - coordinates
  - latitude
  - longitude
  - distance
  - tracking
  - background location
  - foreground location
  - GPS delegate
  - geofence delegate
  - GpsReading
  - location AI tools
  - Shiny.Locations.Extensions.AI
  - AddLocationAITool
  - LocationAITools
  - estimate travel time
  - GpsRequest
  - GeofenceRegion
  - IGpsManager
  - IGeofenceManager
  - AddGps
  - AddLinuxGps
  - LinuxGpsOptions
  - GeoClue
  - GeoClue2
  - GeoClueAccuracyLevel
  - Shiny.Gps.Linux
  - macOS location
  - Linux location
  - AddGeofencing
  - dwell
  - geofence dwell
  - loitering
  - DwellTime
  - GeofenceState.Dwelling
  - reverse geocoding
  - geocoding
  - geocoder
  - address from coordinates
  - IGeocoder
  - AddGeocoding
  - Placemark
  - ReverseGeocode
  - NominatimOptions
  - OpenStreetMap
  - Nominatim
  - motion activity
  - activity recognition
  - walking
  - running
  - cycling
  - automotive
  - stationary
  - IMotionActivityManager
  - IMotionActivityDelegate
  - MotionActivityReading
  - MotionActivityType
  - MotionActivityConfidence
  - AddMotionActivity
  - Shiny.Gps
  - Shiny.Geofencing
  - Shiny.Locations
  - GeofenceExtensions
  - region geofencing
  - unlimited geofences
  - polygon geofence
  - Shiny.Geofencing.Regions
  - Shiny.Geofencing.Regions.DocumentDb
  - AddRegionGeofencing
  - IRegionGeofenceManager
  - IRegionGeofenceDelegate
  - RegionGeofenceConfig
  - RegionChange
  - CurrentRegion
  - IRegionSource
  - RegionMatch
  - AddRegionSet
  - AddGeocoderRegionSet
  - GeocoderRegionLevel
  - GeocoderRegionSource
  - AddDocumentRegionSet
  - DocumentRegionSource
  - MapNorthAmericaData
  - AddNorthAmericaSeeder
  - AddNorthAmericanRegions
  - AddNorthAmericanCities
  - NorthAmericaData
  - GeoRegion
  - GeoCity
---

# Shiny Locations

GPS tracking, geofence monitoring (enter/exit/dwell), reverse geocoding, and motion activity recognition for .NET MAUI, iOS, Android, macOS, Windows, Linux and Blazor applications, with full foreground and background support on mobile.

## When to Use This Skill

Use this skill when the user needs to:

- Track the device GPS position (foreground or background)
- Monitor geofence regions (enter/exit events)
- Detect when the user stays inside a geofence for a period of time (dwell)
- Turn coordinates into a street address (reverse geocoding)
- Calculate distances between geographic positions
- Request location permissions
- Get a single current position reading
- Implement background location tracking delegates
- Detect stationary vs. in-motion state
- Recognize motion activity (walking, running, cycling, automotive, stationary)
- Implement motion activity delegates for background activity processing

## Library Overview

| Property   | Value                        |
|------------|------------------------------|
| NuGet      | `Shiny.Gps` (GPS, motion activity, geocoding), `Shiny.Geofencing` (geofences), `Shiny.Locations` (meta-package referencing both - no code of its own). Blazor WASM GPS is in `Shiny.Gps`'s `net11.0` build (chosen with `OperatingSystem.IsBrowser()`); `Shiny.Locations.Blazor` no longer exists |
| Namespace  | `Shiny.Locations`            |
| Platforms  | GPS: iOS, Mac Catalyst, Android, macOS (AppKit, CoreLocation), Windows (foreground only), Linux (GeoClue2, separate `Shiny.Gps.Linux` package) and Blazor WebAssembly (foreground only). Geofencing: iOS, Mac Catalyst, Android, Windows. **No tvOS target** — `CLMonitor`, `CLMonitorConfiguration` and `CLRegionState` are absent on tvOS, so geofencing cannot be implemented there |
| DI Namespace | `Shiny` (extension methods on `IServiceCollection`) |
| Shared types | `Position` and `Distance` live in `Shiny.Core` (same namespaces as before: `Shiny.Locations.Position`, `Shiny.Distance`) |

`Shiny.Gps` and `Shiny.Geofencing` are independent - neither references the other. Reference only the one
you need; reference `Shiny.Locations` (or both) when you need GPS and geofencing. Every type kept the
`Shiny.Locations` namespace, so no `using` changes are needed after switching packages.

## Setup

### GPS Registration

Register GPS in `MauiProgram.cs`:

```csharp
// GPS without a background delegate (foreground only)
services.AddGps();

// GPS with a background delegate
services.AddGps<MyGpsDelegate>();
```

`AddGps()` / `AddGps<TDelegate>()` exist on every target with the same signature, so shared `net11.0`
startup code compiles once and binds on every platform. The platform switches are separate overloads:
`AddGps(forceUseOldCLManager: true)` (iOS / Mac Catalyst - ignored on macOS, which always uses
`CLLocationManager`) and `AddGps(forceLocationApi: true)` (Android) - guard them with `#if IOS` / `#if ANDROID`.

### macOS (AppKit) GPS Registration

`Shiny.Gps` has a `net11.0-macos` target - same `AddGps()` / `AddGps<TDelegate>()` call, backed by
`CLLocationManager`. Add `NSLocationUsageDescription` (and `NSLocationWhenInUseUsageDescription`) to
`Info.plist`, and the `com.apple.security.personal-information.location` entitlement for a sandboxed app.
macOS differences to respect when generating code:

- A Mac app isn't suspended, so there are no background modes to configure: `GpsBackgroundMode.Standard` uses
  significant location changes, `Realtime` (and `None`) continuous updates. `AppleGpsRequest`'s
  `PausesLocationUpdatesAutomatically`, `ActivityType` and background indicator are ignored.
- macOS has no when-in-use level - a grant is always `AuthorizedAlways`, so a background `RequestAccess` never
  returns `Restricted` for "when-in-use only" and there is no always-upgrade prompt.
- `AddMotionActivity()` is a no-op (no `CMMotionActivityManager` on macOS).
- `AddGeocoding()` uses the native Apple geocoder (MapKit on macOS 26+, `CLGeocoder` below), not Nominatim.
- No geofencing (`Shiny.Geofencing` has no macOS target).

### Linux GPS Registration (GeoClue2)

Reference **`Shiny.Gps.Linux`** (keeps the `Tmds.DBus.Protocol` dependency out of WebAssembly apps) and call
`AddLinuxGps()`. It talks to GeoClue2 on the D-Bus system bus (installed by default on GNOME, KDE and most
desktop distributions), which locates from Wi-Fi, IP, a GPS receiver or a modem. It does nothing on other
operating systems, so it can sit next to `AddGps()` in shared startup code, and it replaces any `IGpsManager`
registered before it - call it **after** `AddGps()`.

```csharp
services.AddGps<MyGpsDelegate>();          // every other platform
services.AddLinuxGps<MyGpsDelegate>(o =>   // Linux - no-op elsewhere
{
    o.DesktopId = "com.mycompany.myapp";   // the app's .desktop file name without extension - GeoClue refuses a client without one
    o.AccuracyLevel = GeoClueAccuracyLevel.Street;        // used when RequestPreciseAccuracy is false (default Street = Wi-Fi)
    o.PreciseAccuracyLevel = GeoClueAccuracyLevel.Exact;  // used when RequestPreciseAccuracy is true (default)
    o.DistanceThresholdMeters = 0;         // GeoClue DistanceThreshold, 0 = every change
    o.TimeThresholdSeconds = 0;            // GeoClue TimeThreshold, 0 = every change
    o.StationaryMetersThreshold = 10;      // GpsReading.IsStationary detection
    o.StationarySecondsThreshold = 30;
});
```

Linux behavior to respect when generating code:

- GeoClue has no "ask" of its own - its agent (GNOME's location privacy setting, the location portal) decides when a
  client starts. `RequestAccess` starts and stops a client to find out; `GetCurrentStatus` returns what was learned
  (`Unknown` until then, `NotSupported` when GeoClue or the system bus is missing, `Denied` when the agent or the
  location switch refused). Always call `RequestAccess` rather than relying on `GetCurrentStatus` on Linux.
- Background modes mean nothing to a desktop process: `Standard`/`Realtime` listen continuously like `None`, but a
  background listener is still persisted and restarted at startup when `AutoRestart` is set.
- `GpsReadingReceived` and `IGpsDelegate`s are called on the D-Bus thread - marshal to the UI thread yourself.
- Unknown altitude is `0`; unknown speed and heading are `-1`.
- `AddMotionActivity()` is a no-op and geofencing is unavailable; `AddGeocoding()` uses Nominatim.

### Blazor WebAssembly GPS Registration

Register GPS in `Program.cs` of a Blazor WebAssembly project. Only foreground GPS
is supported - the browser does not expose background location, geofencing, or
significant-location-change APIs. Background modes on a `GpsRequest` are silently
treated as foreground.

```csharp
builder.Services.AddGps();
// or with a foreground-only delegate:
builder.Services.AddGps<MyGpsDelegate>();
```

Geofencing (`AddGeofencing`) is **not** available on
Blazor WebAssembly. For region-entry behavior on the web, evaluate regions
server-side from GPS reports and notify the client via `Shiny.Push` (Web Push).

### Geofence Registration

Register geofencing in `MauiProgram.cs` (package `Shiny.Geofencing`):

```csharp
using Shiny; // AddGeofencing lives here

// Standard geofencing with a delegate
services.AddGeofencing<MyGeofenceDelegate>();
```

`AddGpsDirectGeofencing` is **gone** - never generate it. It was removed when geofencing moved out of the GPS
package (the code is parked, commented out, in `Shiny.Gps`). On Android without Google Play Services there is
no longer an automatic GPS-direct fallback.

### Geocoding Registration

```csharp
services.AddGeocoding(); // IGeocoder - native on iOS, Mac Catalyst, macOS, Android; OpenStreetMap Nominatim everywhere else

// optional - configure the Nominatim fallback (Windows, Linux, Blazor, Android without Play Services)
services.AddGeocoding(o =>
{
    o.UserAgent = "MyApp/1.0 (contact@myapp.com)"; // public server policy: identify your app (defaults to entry assembly name/version)
    o.Email = "contact@myapp.com";                 // optional contact for heavy use
    o.Language = "en";                              // Accept-Language; defaults to the current UI culture
    // o.BaseUri = new Uri("https://nominatim.mycompany.com/"); // self-hosted server for heavy use
});
```

The public Nominatim server is free but has a usage policy: the registered geocoder serializes requests, spaces them at
least `MinimumRequestInterval` (1s) apart, and caches `CacheSize` (100) results keyed on the position rounded to ~1m.
Results are © OpenStreetMap contributors - show `NominatimOptions.Attribution` wherever they are displayed. Never
generate code that fires reverse geocodes on every GPS reading against the public server; geocode on demand or on a
significant move. The implementation is internal - always inject `IGeocoder`, never `new` up or reference a Nominatim
geocoder type.

### Motion Activity Registration

Register motion activity recognition in `MauiProgram.cs`:

```csharp
// Motion activity without a background delegate
services.AddMotionActivity();

// Motion activity with a background delegate
services.AddMotionActivity<MyMotionActivityDelegate>();
```

> **Platform support:** Motion activity is supported on iOS (CMMotionActivityManager) and Android (Google Play Services Activity Recognition). On Android, Google Play Services must be available — the registration silently no-ops if unavailable. Other platforms (macOS, Windows, Linux, Blazor) are no-ops.

## Code Generation Instructions

When generating code for Shiny.Locations:

1. **Always request permissions before starting listeners.** Call `RequestAccess` and check the returned `AccessState` before calling `StartListener` or `StartMonitoring`.
2. **Use `GpsRequest` factories or constructor** based on the background mode needed:
   - `GpsRequest.Foreground` for foreground-only use (equivalent to `new GpsRequest(GpsBackgroundMode.None)`)
   - `new GpsRequest(GpsBackgroundMode.Standard)` for standard background (iOS: significant location changes; Android: 3-4 updates/hour)
   - `GpsRequest.Realtime(true)` for background realtime with precise accuracy (iOS/Android: updates every 1 second)
3. **Inject `IGpsManager` or `IGeofenceManager`** via constructor injection. Never instantiate managers directly.
4. **Implement `IGpsDelegate`** for background GPS processing, or subclass the abstract `GpsDelegate` base class for built-in filtering by distance/time and stationary detection. The `GpsDelegate` supports minimum filters (`MinimumDistance`, `MinimumTime`) that use AND logic when both are set, and maximum filters (`MaximumDistance`, `MaximumTime`) that use OR logic and always override minimums when crossed.
5. **Implement `IGeofenceDelegate`** for geofence enter/exit events.
6. **Implement `IMotionActivityDelegate`** for background motion activity processing. The delegate receives `MotionActivityReading` with `Activity` (MotionActivityType), `Confidence` (MotionActivityConfidence), and `Timestamp`.
6. **Use `Position` record** with `(latitude, longitude)` -- latitude range is -90 to 90, longitude range is -180 to 180.
7. **Use `Distance` factory methods** -- `Distance.FromMeters()`, `Distance.FromKilometers()`, `Distance.FromMiles()`. Never construct `Distance` directly with kilometers unless intentional.
8. **Use extension methods** for convenience: `GetCurrentPosition()`, `GetLastReadingOrCurrentPosition()`, `IsListening()`, `IsPositionInside()`, `IsInsideRegion()`.
9. **Subscribe to the `GpsReadingReceived` C# event on `IGpsManager` (or `MotionActivityReadingReceived` on `IMotionActivityManager`) for foreground UI updates.** Rx has been removed from Shiny.Locations — use `event EventHandler<GpsReading>` / `event EventHandler<MotionActivityReading>` and always unsubscribe on disappear/dispose to avoid leaks. Delegates remain the way to handle readings while the app is backgrounded.
10. **For `GeofenceRegion`**, always provide a unique `Identifier` string. The `SingleUse` parameter removes the region after the first trigger. `StartMonitoring` with an identifier that is already monitored replaces that region on every platform (iOS 18+ threw before 7.0.0). To register a region idempotently, use the `TryStartMonitoring(region, replaceIfExists)` extension on `IGeofenceManager` — it only starts monitoring if a region with the same identifier isn't already being monitored, and (when `replaceIfExists` is `true`, the default) stops and restarts an existing region so changed position/notification settings take effect. It returns `true` when the region already existed, `false` when it was newly added.
11. **Inject `IMotionActivityManager`** via constructor injection for motion activity features. Call `RequestAccess()` before `StartListener()`, then subscribe to `MotionActivityReadingReceived` for foreground updates or register `IMotionActivityDelegate` for background processing. `IsListening` is persisted and the listener is re-registered at startup after the process is killed, so generate `if (!manager.IsListening) await manager.StartListener();` - never call `StartListener()` unconditionally on app start, it throws when already listening. The same holds for `IGpsManager`: a `Standard`/`Realtime` listener with `AutoRestart` (default `true`) is persisted on every platform and restarted at startup (a plain `GpsRequest` with `AutoRestart: false` was saved as `true` on iOS and Android before 7.0.0), so check `CurrentListener == null` (or the `IsListening()` extension) before starting.
12. **For geofence dwell, set `DwellTime` with an object initializer** - `new GeofenceRegion("id", center, radius) { DwellTime = TimeSpan.FromMinutes(5) }` - and handle `GeofenceState.Dwelling` in `IGeofenceDelegate.OnStatusChanged`. `DwellTime` is not a constructor parameter. Entry and exit are always tracked internally; `NotifyOnEntry`/`NotifyOnExit` only filter what reaches the delegate (set both false for dwell-only). A `SingleUse` region with a `DwellTime` is removed after the dwell, not the entry. No GPS, background mode or extra permission is needed. **Timing differs on iOS:** Android (native loitering delay) and Windows (timer) report `Dwelling` while the user is still inside, but iOS usually reports it at exit, just before `Exited`, because iOS suspends the app between region events. When generating dwell code, don't assume the user is still inside on iOS, and say so if the user's scenario needs an in-place action.
13. **For reverse geocoding, inject `IGeocoder`** (registered by `AddGeocoding()`), check `IsSupported`, then `await geocoder.ReverseGeocode(position, ct)` - it returns `IReadOnlyList<Placemark>` (empty when nothing matched) with `FormattedAddress`, `Thoroughfare`, `Locality`, `AdministrativeArea`, `PostalCode`, `CountryCode`, etc. It needs network access. On Windows, Linux, Blazor and Android without Play Services the registered `IGeocoder` uses OpenStreetMap Nominatim (macOS uses the native Apple geocoder from 7.1.0) - display `NominatimOptions.Attribution` with its results, and expect calls to queue at one per second against the public server.

## Conventions

- All async operations return `Task` or `Task<T>`.
- The convenience extension methods live on `Shiny.Locations.LocationExtensions` (renamed from `Extensions` in 5.2.5 — a type named `Shiny.Locations.Extensions` collides with the `Shiny.Locations.Extensions.AI` namespace and produces CS0434 in consuming projects). They are extension methods, so call sites are unaffected. Since the package split, the geofence ones (`TryStartMonitoring`, `IsPositionInside`) live on `Shiny.Locations.GeofenceExtensions` in `Shiny.Geofencing`; the GPS ones stay on `LocationExtensions` in `Shiny.Gps`.
- Foreground observation uses C# `event EventHandler<T>` on the managers (`GpsReadingReceived`, `MotionActivityReadingReceived`) — Rx is no longer used in Shiny.Locations.
- The `GpsBackgroundMode` enum controls background behavior: `None` (foreground), `Standard` (periodic), `Realtime` (continuous).
- `IGpsDelegate`s receive every reading in every background mode on every platform (Android ran them only for `Realtime` before 7.0.0).
- `AppleGpsRequest.DistanceFilterMeters` and `UseSignificantLocationChanges` are honored on iOS / Mac Catalyst / macOS (on iOS 18+ the distance filter is applied by Shiny, since `CLLocationUpdater` has none).
- iOS below 18 monitors at most 20 regions per app (geofences and beacon regions together): `StartMonitoring` throws `InvalidOperationException` past that instead of silently never watching the region.
- `IGeofenceManager.CurrentStatus` / `IGpsManager.GetCurrentStatus` never prompt - on Windows they read the permission without calling `RequestAccessAsync`, so call `RequestAccess` to ask.
- `GeofenceState` enum values: `Unknown`, `Entered`, `Exited`, `Dwelling` (only for regions with a `DwellTime`).
- `AccessState` is from Shiny.Core and includes `Available`, `Denied`, `Disabled`, `Restricted`, `NotSupported`, `Unknown`.

## Best Practices

- Always check `AccessState` before starting GPS or geofence monitoring. Handle `Denied` and `Restricted` states gracefully with user-facing messaging.
- Prefer `GpsBackgroundMode.Standard` over `Realtime` to conserve battery. Only use `Realtime` when continuous tracking is required.
- Stop listeners when they are no longer needed (`StopListener()` / `StopAllMonitoring()`).
- Use the abstract `GpsDelegate` base class instead of implementing `IGpsDelegate` directly. It provides `MinimumDistance`, `MinimumTime` (AND when both set), `MaximumDistance`, `MaximumTime` (OR, overrides minimums) filtering, and stationary detection out of the box.
- For single position reads, use the `GetCurrentPosition()` extension method which handles starting/stopping the listener automatically.
- Unsubscribe from `GpsReadingReceived` / `MotionActivityReadingReceived` when the view/page is no longer active (pair `+=` with `-=` on disappear/dispose).
- On iOS, configure `NSLocationWhenInUseUsageDescription` and `NSLocationAlwaysAndWhenInUseUsageDescription` in `Info.plist`. Word the "always" strings for what background access actually does — following the user with the app closed — rather than reusing the when-in-use sentence.
- **On iOS, call `RequestAccess` with the background request from the feature that needs it, not at launch.** Authorization escalates (when-in-use first, always only as an upgrade) and the system presents the upgrade prompt once, so the moment the user turns the feature on is the only moment the dialog can be explained. Requires 7.0.0+ for both `IGpsManager` and `IGeofenceManager`: before that, an always `CLServiceSession` on iOS 18+ (which never shows the upgrade itself) meant no upgrade prompt, the geofence manager returned `Denied` for when-in-use and kept returning it after the user granted always, and iOS below 18 never asked for the upgrade. A background request returns `Restricted` while only when-in-use is granted. The first upgrade request waits up to 30 seconds for the user's answer (the app gets no signal while the prompt is up); later calls return straight away, so don't put a timeout shorter than that around the first `RequestAccess`.
- **Treat `AccessState.Restricted` as "granted, but not for what you asked for", not as a refusal.** It is what `AuthorizedWhenInUse` reports as when a background request asks about it, and also what reduced accuracy reports as. A user-facing message should say which journeys/readings are lost and offer the upgrade, rather than reading like a blocked permission.
- On Android, configure `ACCESS_FINE_LOCATION`, `ACCESS_COARSE_LOCATION`, and `ACCESS_BACKGROUND_LOCATION` permissions in `AndroidManifest.xml`.
- On iOS, add `NSMotionUsageDescription` to `Info.plist` when using motion activity recognition.
- On Android, motion activity recognition requires `com.google.android.gms.permission.ACTIVITY_RECOGNITION` permission and Google Play Services.

## Region Geofencing (Shiny.Geofencing.Regions)

GPS-driven geofencing with **no region limit** and any shape - use it instead of `AddGeofencing` when the app needs
more than 20 (iOS) / 60 (Android) regions, polygons, or address-level regions (country/state/city). It runs background GPS
(it **owns** the `IGpsManager` listener - don't also start GPS yourself with a different request), filters readings
(`MinimumDistance` 300m / `MinimumTime` 1 min by default, optional `MaximumDistance`/`MaximumTime` safety nets), and asks
each **region set**'s `IRegionSource` which region the reading is in. Each set tracks one current region, persisted, so
restarts don't replay entries. iOS, Android, Mac Catalyst (`AddRegionGeofencing` is `#if APPLE || ANDROID`; the types
compile on `net11.0`). Namespace `Shiny.Locations`; registration in `Shiny`.

```csharp
builder.Services.AddRegionGeofencing<MyRegionDelegate>(cfg => cfg
    // built in: reverse geocoding through IGeocoder - online only; AddRegionGeofencing registers AddGeocoding() if missing
    .AddGeocoderRegionSet("states", GeocoderRegionLevel.AdministrativeArea)   // id "US-Colorado", region = Placemark
    .AddGeocoderRegionSet("cities", GeocoderRegionLevel.Locality)             // id "US-Colorado-Denver"
    // Shiny.Geofencing.Regions.DocumentDb - SQLite document store registered with AddDocumentStore
    .AddDocumentRegionSet<Zone>("zones", z => z.Id, z => z.Name, filter: z => z.Active)   // polygons: containment
    .AddDocumentRegionSet<Store>("stores", s => s.Id, s => s.Name, withinMeters: 500)    // points: proximity
    // custom source
    .AddRegionSet("custom", sp => new MyRegionSource(sp.GetRequiredService<IMyApi>()))
);

public class MyRegionDelegate : IRegionGeofenceDelegate
{
    public Task OnRegionChanged(RegionChange change)   // exit before entry when moving A -> B in one set
    {
        var zone = change.RegionAs<Zone>();             // the document / Placemark / your object
        return Task.CompletedTask;
    }
}

// IRegionGeofenceManager: RequestAccess(), Start() (idempotent), Stop() (forgets current regions), IsStarted,
// GetCurrent() (resolves every set from the last reading; no events, no state change)
```

- Document regions need `opts.ConfigureDocument<T>(cfg => cfg.MapSpatialProperty(nameof(T.Geo), x => x.Geo))` in a
  SQLite `AddDocumentStore` (`SqliteDatabaseProvider`). `idSelector` must return the document's store id (exits after a
  restart re-read the region by it). `storeName:` picks a keyed store.
- US/Canada dataset (in `.DocumentDb`): `opts.MapNorthAmericaData()`, `services.AddNorthAmericaSeeder()`, then
  `cfg.AddNorthAmericanRegions()` (set "regions", ids `US-CO`/`CA-ON`) and `cfg.AddNorthAmericanCities(withinMeters: 25_000)`
  (set "cities", ids `US-CO-denver`). Types `GeoRegion`/`GeoCity`/`NorthAmericaData` live in `Shiny.Locations.NorthAmerica`.
- Geocoder sets: a failed lookup keeps the current region (no false exit while offline); sets share one lookup per
  reading; exits after a restart carry only `RegionId` (`Region`/`RegionName` null). Respect geocoder rate limits -
  keep `MinimumTime` >= 1 minute.
- A throwing `IRegionSource.FindRegion` is logged and leaves that set unchanged; other sets still evaluate.
- Replaces `Shiny.DocumentDb.Geofencing` (`AddDocumentGeofencing` -> `AddRegionGeofencing`, `AddRegionSet<T>` ->
  `AddDocumentRegionSet<T>`), `Shiny.DocumentDb.Geo` and `Shiny.Spatial.Geofencing`.

## AI Tool Integration (Shiny.Locations.Extensions.AI)

The optional `Shiny.Locations.Extensions.AI` package exposes `IGpsManager` as **read-only** `Microsoft.Extensions.AI` tool functions (`AIFunction`s) for LLM agents — the agent can learn where the user is and reason about distance/time to a destination, but never writes location data. You opt-in via `AddGps()` (an allow-list you control on behalf of the agent — **not** an OS permission prompt; location permission must already be granted). AOT-compatible (hand-built schemas, `JsonNode` results — no reflection).

```csharp
using Shiny.Locations;
using Shiny.Locations.Extensions.AI;

builder.Services.AddGps();                                  // registers IGpsManager
builder.Services.AddLocationAITool();                       // read-only; there is no write capability for GPS

// resolve the bundle and pass the tools to any IChatClient
var tools = sp.GetRequiredService<LocationAITools>().Tools;
var response = await chatClient.GetResponseAsync(
    messages,
    new ChatOptions { Tools = [.. tools] }
);
```

Key types:
- `AddLocationAITool()` — parameterless DI extension. GPS is read-only, so there is no builder or capability to opt-in to; the call registers all three location tools.
- `LocationAITools` — resolve from DI; `.Tools` is `IReadOnlyList<AITool>`.

Generated tools: `get_current_location` (last cached fix — lat/lng, accuracy, altitude, speed, heading, timestamp), `get_distance_to` (great-circle distance + compass bearing to a destination lat/lng), `estimate_travel_time` (`mode` walking/cycling/transit/driving or a `speedKmh` override → straight-line ETA).

> The tools read the **last cached GPS reading**; start a listener or ensure a recent fix exists first. Distances and travel times are **great-circle (straight-line) estimates** — not routed ETAs with roads/traffic — and the tool results say so. Location permission should already be granted before invoking the agent.

> Combine with the app's own functions: `Shiny.AppFunctions.Extensions.AI` (`AddAppFunctionAITools`, see the **shiny-appfunctions** skill) exposes the `[AppFunction]`s Siri and Gemini call as tools in the same `*AITools` bundle shape - concatenate the `.Tools` lists into one `ChatOptions.Tools`.

## Reference Files

- [API Reference](reference/api-reference.md)
