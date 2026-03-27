---
name: shiny-locations
description: GPS tracking and geofence monitoring for .NET MAUI, iOS, and Android using Shiny.Locations
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
---

# Shiny Locations

GPS tracking and geofence monitoring for .NET MAUI, iOS, and Android applications with full foreground and background support.

## When to Use This Skill

Use this skill when the user needs to:

- Track the device GPS position (foreground or background)
- Monitor geofence regions (enter/exit events)
- Calculate distances between geographic positions
- Request location permissions
- Get a single current position reading
- Implement background location tracking delegates
- Detect stationary vs. in-motion state

## Library Overview

| Property   | Value                        |
|------------|------------------------------|
| NuGet      | `Shiny.Locations`            |
| Namespace  | `Shiny.Locations`            |
| Platforms  | iOS, Android, WebAssembly    |
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

### Geofence Registration

Register geofencing in `MauiProgram.cs`:

```csharp
// Standard geofencing with a delegate
services.AddGeofencing<MyGeofenceDelegate>();

// GPS-direct geofencing (uses realtime GPS - battery intensive)
services.AddGpsDirectGeofencing<MyGeofenceDelegate>();
```

## Code Generation Instructions

When generating code for Shiny.Locations:

1. **Always request permissions before starting listeners.** Call `RequestAccess` and check the returned `AccessState` before calling `StartListener` or `StartMonitoring`.
2. **Use `GpsRequest` factories or constructor** based on the background mode needed:
   - `GpsRequest.Foreground` for foreground-only use (equivalent to `new GpsRequest(GpsBackgroundMode.None)`)
   - `new GpsRequest(GpsBackgroundMode.Standard)` for standard background (iOS: significant location changes; Android: 3-4 updates/hour)
   - `GpsRequest.Realtime(true)` for background realtime with precise accuracy (iOS/Android: updates every 1 second)
3. **Inject `IGpsManager` or `IGeofenceManager`** via constructor injection. Never instantiate managers directly.
4. **Implement `IGpsDelegate`** for background GPS processing, or subclass the abstract `GpsDelegate` base class for built-in filtering by distance/time and stationary detection.
5. **Implement `IGeofenceDelegate`** for geofence enter/exit events.
6. **Use `Position` record** with `(latitude, longitude)` -- latitude range is -90 to 90, longitude range is -180 to 180.
7. **Use `Distance` factory methods** -- `Distance.FromMeters()`, `Distance.FromKilometers()`, `Distance.FromMiles()`. Never construct `Distance` directly with kilometers unless intentional.
8. **Use extension methods** for convenience: `GetCurrentPosition()`, `GetLastReadingOrCurrentPosition()`, `IsListening()`, `IsPositionInside()`, `IsInsideRegion()`.
9. **Use `WhenReading()` for UI bindings** (observable stream). Use delegates for background processing.
10. **For `GeofenceRegion`**, always provide a unique `Identifier` string. The `SingleUse` parameter removes the region after the first trigger.

## Conventions

- All async operations return `Task` or `Task<T>`.
- Reactive streams use `IObservable<T>` from System.Reactive.
- The `GpsBackgroundMode` enum controls background behavior: `None` (foreground), `Standard` (periodic), `Realtime` (continuous).
- `GeofenceState` enum values: `Unknown`, `Entered`, `Exited`.
- `AccessState` is from Shiny.Core and includes `Available`, `Denied`, `Disabled`, `Restricted`, `NotSupported`, `Unknown`.

## Best Practices

- Always check `AccessState` before starting GPS or geofence monitoring. Handle `Denied` and `Restricted` states gracefully with user-facing messaging.
- Prefer `GpsBackgroundMode.Standard` over `Realtime` to conserve battery. Only use `Realtime` when continuous tracking is required.
- Stop listeners when they are no longer needed (`StopListener()` / `StopAllMonitoring()`).
- Use the abstract `GpsDelegate` base class instead of implementing `IGpsDelegate` directly. It provides `MinimumDistance`, `MinimumTime` filtering, and stationary detection out of the box.
- For single position reads, use the `GetCurrentPosition()` extension method which handles starting/stopping the listener automatically.
- Dispose of `IObservable` subscriptions from `WhenReading()` when the view/page is no longer active.
- On iOS, configure `NSLocationWhenInUseUsageDescription` and `NSLocationAlwaysAndWhenInUseUsageDescription` in `Info.plist`.
- On Android, configure `ACCESS_FINE_LOCATION`, `ACCESS_COARSE_LOCATION`, and `ACCESS_BACKGROUND_LOCATION` permissions in `AndroidManifest.xml`.

## Reference Files

- [API Reference](reference/api-reference.md)
