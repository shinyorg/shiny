---
name: shiny-locations
description: GPS tracking, geofence monitoring, and motion activity recognition for .NET MAUI, iOS, and Android using Shiny.Locations
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
  - GpsRequest
  - GeofenceRegion
  - IGpsManager
  - IGeofenceManager
  - AddGps
  - AddGeofencing
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
---

# Shiny Locations

GPS tracking, geofence monitoring, and motion activity recognition for .NET MAUI, iOS, and Android applications with full foreground and background support.

## When to Use This Skill

Use this skill when the user needs to:

- Track the device GPS position (foreground or background)
- Monitor geofence regions (enter/exit events)
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
| NuGet      | `Shiny.Locations` (MAUI), `Shiny.Locations.Blazor` (Blazor WASM) |
| Namespace  | `Shiny.Locations`            |
| Platforms  | iOS, Android, Windows, Blazor WebAssembly (foreground GPS only) |
| DI Namespace | `Shiny` (extension methods on `IServiceCollection`) |
| Support Library | `Shiny.Support.Locations` (provides `Position` and `Distance`) |

## Setup

### GPS Registration

Register GPS in `MauiProgram.cs`:

```csharp
// GPS without a background delegate (foreground only)
services.AddGps();

// GPS with a background delegate
services.AddGps<MyGpsDelegate>();
```

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

Geofencing (`AddGeofencing`, `AddGpsDirectGeofencing`) is **not** available in
`Shiny.Locations.Blazor`. For region-entry behavior on the web, evaluate regions
server-side from GPS reports and notify the client via `Shiny.Push.Blazor`.

### Geofence Registration

Register geofencing in `MauiProgram.cs`:

```csharp
// Standard geofencing with a delegate
services.AddGeofencing<MyGeofenceDelegate>();

// GPS-direct geofencing (uses realtime GPS - battery intensive)
services.AddGpsDirectGeofencing<MyGeofenceDelegate>();
```

### Motion Activity Registration

Register motion activity recognition in `MauiProgram.cs`:

```csharp
// Motion activity without a background delegate
services.AddMotionActivity();

// Motion activity with a background delegate
services.AddMotionActivity<MyMotionActivityDelegate>();
```

> **Platform support:** Motion activity is supported on iOS (CMMotionActivityManager) and Android (Google Play Services Activity Recognition). On Android, Google Play Services must be available — the registration silently no-ops if unavailable. Other platforms (Windows, Blazor) are no-ops.

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
10. **For `GeofenceRegion`**, always provide a unique `Identifier` string. The `SingleUse` parameter removes the region after the first trigger. To register a region idempotently, use the `TryStartMonitoring(region, replaceIfExists)` extension on `IGeofenceManager` — it only starts monitoring if a region with the same identifier isn't already being monitored, and (when `replaceIfExists` is `true`, the default) stops and restarts an existing region so changed position/notification settings take effect. It returns `true` when the region already existed, `false` when it was newly added.
11. **Inject `IMotionActivityManager`** via constructor injection for motion activity features. Call `RequestAccess()` before `StartListener()`, then subscribe to `MotionActivityReadingReceived` for foreground updates or register `IMotionActivityDelegate` for background processing.

## Conventions

- All async operations return `Task` or `Task<T>`.
- Foreground observation uses C# `event EventHandler<T>` on the managers (`GpsReadingReceived`, `MotionActivityReadingReceived`) — Rx is no longer used in Shiny.Locations.
- The `GpsBackgroundMode` enum controls background behavior: `None` (foreground), `Standard` (periodic), `Realtime` (continuous).
- `GeofenceState` enum values: `Unknown`, `Entered`, `Exited`.
- `AccessState` is from Shiny.Core and includes `Available`, `Denied`, `Disabled`, `Restricted`, `NotSupported`, `Unknown`.

## Best Practices

- Always check `AccessState` before starting GPS or geofence monitoring. Handle `Denied` and `Restricted` states gracefully with user-facing messaging.
- Prefer `GpsBackgroundMode.Standard` over `Realtime` to conserve battery. Only use `Realtime` when continuous tracking is required.
- Stop listeners when they are no longer needed (`StopListener()` / `StopAllMonitoring()`).
- Use the abstract `GpsDelegate` base class instead of implementing `IGpsDelegate` directly. It provides `MinimumDistance`, `MinimumTime` (AND when both set), `MaximumDistance`, `MaximumTime` (OR, overrides minimums) filtering, and stationary detection out of the box.
- For single position reads, use the `GetCurrentPosition()` extension method which handles starting/stopping the listener automatically.
- Unsubscribe from `GpsReadingReceived` / `MotionActivityReadingReceived` when the view/page is no longer active (pair `+=` with `-=` on disappear/dispose).
- On iOS, configure `NSLocationWhenInUseUsageDescription` and `NSLocationAlwaysAndWhenInUseUsageDescription` in `Info.plist`.
- On Android, configure `ACCESS_FINE_LOCATION`, `ACCESS_COARSE_LOCATION`, and `ACCESS_BACKGROUND_LOCATION` permissions in `AndroidManifest.xml`.
- On iOS, add `NSMotionUsageDescription` to `Info.plist` when using motion activity recognition.
- On Android, motion activity recognition requires `com.google.android.gms.permission.ACTIVITY_RECOGNITION` permission and Google Play Services.

## Reference Files

- [API Reference](reference/api-reference.md)
