# Shiny.Locations API Reference

## Installation

For MAUI / native targets:

```xml
<PackageReference Include="Shiny.Locations" Version="4.*" />
```

For Blazor WebAssembly:

```xml
<PackageReference Include="Shiny.Locations.Blazor" Version="4.*" />
```

The support library `Shiny.Support.Locations` is included transitively and provides the `Position` and `Distance` types.

> **Blazor / Web limitations.** `Shiny.Locations.Blazor` only implements `IGpsManager`, and only for foreground use via `navigator.geolocation`. There is no `IGeofenceManager` (the browser has no Geofence API), no significant-location-change API, and no way to keep the page alive in the background. Background modes on a `GpsRequest` are accepted but logged and treated as foreground; `IGpsDelegate` is invoked but only while the tab is alive.

## Namespaces

```csharp
using Shiny;            // Distance, DI extension methods
using Shiny.Locations;  // All location types and interfaces
```

---

## Enums

### GeofenceState

```csharp
namespace Shiny.Locations;

public enum GeofenceState
{
    Unknown = 0,
    Entered = 1,
    Exited = 2
}
```

### GpsBackgroundMode

```csharp
namespace Shiny.Locations;

public enum GpsBackgroundMode
{
    /// No background mode
    None,

    /// iOS: Significant Location Changes
    /// Android: BACKGROUND - receive 3-4 updates per hour
    Standard,

    /// iOS: Full background request - Updates every 1 second
    /// Android: Foreground Service - Updates every 1 second
    Realtime
}
```

### MotionActivityType

```csharp
namespace Shiny.Locations;

public enum MotionActivityType
{
    Unknown = 0,
    Stationary = 1,
    Walking = 2,
    Running = 3,
    Cycling = 4,
    Automotive = 5
}
```

### MotionActivityConfidence

```csharp
namespace Shiny.Locations;

public enum MotionActivityConfidence
{
    Low,
    Medium,
    High
}
```

---

## Records

### Position

```csharp
namespace Shiny.Locations;

public record Position(double Latitude, double Longitude)
{
    // Latitude must be -90 to 90, Longitude must be -180 to 180 (validated on construction)

    Distance GetDistanceTo(Position other);
    double GetCompassBearingTo(Position to);

    static double ToRad(double degrees);
    static double ToDegrees(double radians);
    static double ToBearing(double radians);
}
```

### Distance

```csharp
namespace Shiny;

public record Distance(double TotalKilometers)
{
    // Constants
    const double MILES_TO_KM = 1.60934;
    const double KM_TO_MILES = 0.621371;
    const int KM_TO_METERS = 1000;

    // Computed properties
    double TotalMiles { get; }
    double TotalMeters { get; }

    // Factory methods
    static Distance FromMiles(int miles);
    static Distance FromMiles(double miles);
    static Distance FromMeters(double meters);
    static Distance FromKilometers(double km);

    // Operators
    static bool operator >(Distance x, Distance y);
    static bool operator <(Distance x, Distance y);
    static bool operator >=(Distance x, Distance y);
    static bool operator <=(Distance x, Distance y);

    // Static methods
    static Distance Between(Position one, Position two);
}
```

### GpsReading

```csharp
namespace Shiny.Locations;

public record GpsReading(
    Position Position,
    double PositionAccuracy,
    DateTimeOffset Timestamp,
    double Heading,
    double HeadingAccuracy,
    double Altitude,
    double Speed,
    double SpeedAccuracy,
    int Floor = 0,
    bool IsStationary = false
);
```

### GpsRequest

```csharp
namespace Shiny.Locations;

public record GpsRequest(
    GpsBackgroundMode BackgroundMode = GpsBackgroundMode.None,
    bool RequestPreciseAccuracy = false
)
{
    static GpsRequest Foreground { get; }   // => new(GpsBackgroundMode.None, false)
    static GpsRequest Background { get; }   // => new(GpsBackgroundMode.None, false)
    static GpsRequest Realtime(bool requestPreciseAccuracy); // => new(GpsBackgroundMode.Realtime, ...)
}
```

### GeofenceRegion

```csharp
namespace Shiny.Locations;

public record GeofenceRegion(
    string Identifier,
    Position Center,
    Distance Radius,
    bool SingleUse = false,
    bool NotifyOnEntry = true,
    bool NotifyOnExit = true
) : IRepositoryEntity;
```

### MotionActivityReading

```csharp
namespace Shiny.Locations;

public record MotionActivityReading(
    MotionActivityType Activity,
    MotionActivityConfidence Confidence,
    DateTimeOffset Timestamp
);
```

### LocationPermissionResult

```csharp
namespace Shiny.Locations;

public record LocationPermissionResult(
    AccessState Access,
    bool? HasBackground,
    bool? HasFineAccess
);
```

---

## Interfaces

### IGpsManager

```csharp
namespace Shiny.Locations;

public interface IGpsManager
{
    /// If the device is currently listening to GPS broadcasts
    GpsRequest? CurrentListener { get; }

    /// Get the current access state
    AccessState GetCurrentStatus(GpsRequest request);

    /// Request access to use GPS hardware
    Task<AccessState> RequestAccess(GpsRequest request);

    /// Gets the last reading (optionally waiting up to a timeout for one). Throws if access not granted.
    Task<GpsReading?> GetLastReading(TimeSpan? timeout = null);

    /// Fired when a GPS reading is received (foreground only). Use delegates for background.
    event EventHandler<GpsReading> GpsReadingReceived;

    /// Start the GPS listener
    Task StartListener(GpsRequest request);

    /// Stop the GPS listener
    Task StopListener();
}
```

### IGeofenceManager

```csharp
namespace Shiny.Locations;

public interface IGeofenceManager
{
    /// Gets the current permission status
    AccessState CurrentStatus { get; }

    /// Requests appropriate platform permissions
    Task<AccessState> RequestAccess();

    /// Current set of geofences being monitored
    IList<GeofenceRegion> GetMonitorRegions();

    /// Start monitoring a geofence
    Task StartMonitoring(GeofenceRegion region);

    /// Stop monitoring a geofence
    Task StopMonitoring(string identifier);

    /// Stop monitoring all active geofences
    Task StopAllMonitoring();

    /// Request the current state of a geofence region
    Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default);
}
```

### IGpsDelegate

```csharp
namespace Shiny.Locations;

public interface IGpsDelegate
{
    /// Fired when the GPS reading has changed
    Task OnReading(GpsReading reading);
}
```

### IGeofenceDelegate

```csharp
namespace Shiny.Locations;

public interface IGeofenceDelegate
{
    /// Fired when the geofence region status has changed
    Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region);
}
```

### IMotionActivityManager

```csharp
namespace Shiny.Locations;

public interface IMotionActivityManager
{
    /// If the manager is currently listening for activity changes
    bool IsListening { get; }

    /// Get the current access state for motion activity recognition
    AccessState GetCurrentStatus();

    /// Request access to motion activity recognition
    Task<AccessState> RequestAccess();

    /// Gets the last known motion activity reading, or queries the platform for the current one
    Task<MotionActivityReading?> GetLastReading();

    /// Fired when a motion activity reading is received (foreground). Use delegates for background.
    event EventHandler<MotionActivityReading> MotionActivityReadingReceived;

    /// Start listening for motion activity changes
    Task StartListener();

    /// Stop listening for motion activity changes
    Task StopListener();
}
```

### IMotionActivityDelegate

```csharp
namespace Shiny.Locations;

public interface IMotionActivityDelegate
{
    /// Fired when a motion activity reading is received
    Task OnReading(MotionActivityReading reading);
}
```

---

## Abstract Base Classes

### GpsDelegate

A base class that implements `IGpsDelegate` with built-in filtering and stationary detection.

```csharp
namespace Shiny.Locations;

public abstract class GpsDelegate(ILogger logger) : NotifyPropertyChanged, IGpsDelegate
{
    // Minimum threshold filters (AND when both set, single check if only one set)
    Distance? MinimumDistance { get; set; }
    TimeSpan? MinimumTime { get; set; }

    // Maximum threshold filters (OR - if either is crossed, always fires regardless of minimums)
    Distance? MaximumDistance { get; set; }
    TimeSpan? MaximumTime { get; set; }

    // Stationary detection configuration
    protected int StationaryMetersThreshold { get; set; }   // default: 10
    protected int StationarySecondsThreshold { get; set; }  // default: 30
    protected bool DetectStationary { get; set; }            // default: false

    // State
    GpsReading? LastReading { get; set; }
    GpsReading? MostRecentReading { get; set; }
    bool IsStationary { get; }

    // Override this method to handle filtered GPS readings
    protected abstract Task OnGpsReading(GpsReading reading);
}
```

**Filtering behavior:**

- **Minimums (AND):** When both `MinimumDistance` and `MinimumTime` are set, *both* thresholds must be met before `OnGpsReading` fires. When only one is set, that single threshold is checked.
- **Maximums (OR):** When `MaximumDistance` or `MaximumTime` is set, crossing *either* threshold always fires `OnGpsReading`, regardless of whether minimum thresholds are met. This is useful as a safety net to ensure readings are never suppressed for too long.
- **Priority:** Maximum thresholds are evaluated first. If a maximum fires, minimum checks are skipped entirely.

### GpsGeofenceDelegate

Uses GPS readings to drive geofence state changes. Registered automatically when using `AddGpsDirectGeofencing`.

```csharp
namespace Shiny.Locations;

public class GpsGeofenceDelegate : NotifyPropertyChanged, IGpsDelegate
{
    Dictionary<string, GeofenceState> CurrentStates { get; }

    Task OnReading(GpsReading reading);
}
```

---

## Extension Methods

### IGpsManager Extensions

```csharp
namespace Shiny.Locations;

public static class Extensions
{
    /// Returns true if there is a current GPS listener running
    static bool IsListening(this IGpsManager manager);

    /// Requests a single GPS reading - starts & stops the listener if not already running
    static Task<GpsReading?> GetCurrentPosition(
        this IGpsManager gpsManager,
        CancellationToken cancellationToken = default
    );

    /// Gets the last reading (optionally filtered by age), or falls back to current position
    static Task<GpsReading?> GetLastReadingOrCurrentPosition(
        this IGpsManager gpsManager,
        DateTime? maxAgeOfLastReading = null,
        CancellationToken cancellationToken = default
    );

    /// Checks if the current GPS position is inside the specified region
    static Task<bool?> IsInsideRegion(
        this IGpsManager gpsManager,
        Position center,
        Distance radius,
        CancellationToken cancelToken = default
    );
}
```

### IGeofenceManager Extensions

```csharp
namespace Shiny.Locations;

public static class Extensions
{
    /// Starts monitoring a region only if its identifier isn't already monitored.
    /// When replaceIfExists is true (default), an existing region with the same
    /// identifier is stopped and restarted so changed position/notification
    /// settings take effect. Returns true if the region already existed,
    /// false if it was newly added.
    static Task<bool> TryStartMonitoring(
        this IGeofenceManager geofenceManager,
        GeofenceRegion region,
        bool replaceIfExists = true
    );
}
```

### GeofenceRegion Extensions

```csharp
namespace Shiny.Locations;

public static class Extensions
{
    /// Determines if the provided position is inside the geofence region
    static bool IsPositionInside(this GeofenceRegion region, Position position);
}
```

---

## Dependency Injection Registration

All registration extension methods are in the `Shiny` namespace on `IServiceCollection`.

### GPS Registration

```csharp
// GPS without a background delegate
services.AddGps();

// GPS with a background delegate
services.AddGps<MyGpsDelegate>();

// Non-generic version
services.AddGps(typeof(MyGpsDelegate));
```

**Platform-specific parameters:**
- **iOS/macOS:** `bool forceUseOldCLManager = false` -- forces use of the legacy CLLocationManager (not recommended on iOS 18+)
- **Android:** `bool forceLocationApi = false` -- bypasses FusedLocationProvider and uses the legacy Location API

### Geofence Registration

```csharp
// Standard geofencing with a delegate
services.AddGeofencing<MyGeofenceDelegate>();

// Non-generic version
services.AddGeofencing(typeof(MyGeofenceDelegate));

// GPS-direct geofencing (uses realtime GPS, battery intensive)
services.AddGpsDirectGeofencing<MyGeofenceDelegate>();

// Non-generic version
services.AddGpsDirectGeofencing(typeof(MyGeofenceDelegate));
```

### Motion Activity Registration

```csharp
// Motion activity without a background delegate
services.AddMotionActivity();

// Motion activity with a background delegate
services.AddMotionActivity<MyMotionActivityDelegate>();

// Non-generic version
services.AddMotionActivity(typeof(MyMotionActivityDelegate));
```

**Platform notes:**
- **iOS/macOS:** Uses `CMMotionActivityManager`. Requires `NSMotionUsageDescription` in `Info.plist`.
- **Android:** Uses Google Play Services Activity Recognition API. Requires `com.google.android.gms.permission.ACTIVITY_RECOGNITION`. Registration silently no-ops if Google Play Services is unavailable.
- **Other platforms:** Registration is a no-op.

---

## Usage Examples

### Request Permission and Start GPS

```csharp
public class MyViewModel
{
    readonly IGpsManager gpsManager;

    public MyViewModel(IGpsManager gpsManager)
    {
        this.gpsManager = gpsManager;
    }

    public async Task StartTracking()
    {
        var request = GpsRequest.Foreground;
        var access = await this.gpsManager.RequestAccess(request);

        if (access != AccessState.Available)
        {
            // Handle denied/restricted
            return;
        }

        await this.gpsManager.StartListener(request);
        this.gpsManager.GpsReadingReceived += this.OnGpsReadingReceived;
    }

    void OnGpsReadingReceived(object? sender, GpsReading reading)
    {
        var lat = reading.Position.Latitude;
        var lng = reading.Position.Longitude;
        var speed = reading.Speed;
    }

    public async Task StopTracking()
    {
        this.gpsManager.GpsReadingReceived -= this.OnGpsReadingReceived;
        await this.gpsManager.StopListener();
    }
}
```

### Get a Single Current Position

```csharp
var reading = await gpsManager.GetCurrentPosition();
var position = reading?.Position;
```

### Get Last Reading or Current Position

```csharp
// Accept readings up to 5 minutes old, otherwise get a fresh one
var reading = await gpsManager
    .GetLastReadingOrCurrentPosition(DateTime.UtcNow.AddMinutes(-5));
```

### Background GPS Delegate

```csharp
public class MyGpsDelegate : GpsDelegate
{
    readonly ILogger<MyGpsDelegate> logger;

    public MyGpsDelegate(ILogger<MyGpsDelegate> logger) : base(logger)
    {
        // Both minimums must be met (AND) before OnGpsReading fires
        this.MinimumDistance = Distance.FromMeters(100);
        this.MinimumTime = TimeSpan.FromSeconds(30);

        // Safety net: if either maximum is crossed, always fire (OR) regardless of minimums
        this.MaximumDistance = Distance.FromKilometers(1);
        this.MaximumTime = TimeSpan.FromMinutes(5);

        this.DetectStationary = true;
    }

    protected override async Task OnGpsReading(GpsReading reading)
    {
        this.logger.LogInformation(
            "GPS: {Lat}, {Lng} - Speed: {Speed}",
            reading.Position.Latitude,
            reading.Position.Longitude,
            reading.Speed
        );

        // Send to server, update local DB, etc.
    }
}

// Registration
services.AddGps<MyGpsDelegate>();
```

### Monitor Geofences

```csharp
public class MyGeofenceDelegate : IGeofenceDelegate
{
    readonly ILogger<MyGeofenceDelegate> logger;

    public MyGeofenceDelegate(ILogger<MyGeofenceDelegate> logger)
    {
        this.logger = logger;
    }

    public Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
    {
        this.logger.LogInformation(
            "Geofence {Id}: {State}",
            region.Identifier,
            newStatus
        );
        return Task.CompletedTask;
    }
}

// Registration
services.AddGeofencing<MyGeofenceDelegate>();
```

### Start Monitoring a Geofence Region

```csharp
var access = await geofenceManager.RequestAccess();
if (access == AccessState.Available)
{
    var region = new GeofenceRegion(
        "my-office",
        new Position(43.6532, -79.3832),
        Distance.FromMeters(200),
        SingleUse: false,
        NotifyOnEntry: true,
        NotifyOnExit: true
    );
    await geofenceManager.StartMonitoring(region);
}
```

### Start Monitoring Idempotently

```csharp
var region = new GeofenceRegion(
    "my-office",
    new Position(43.6532, -79.3832),
    Distance.FromMeters(200)
);

// Safe to call repeatedly (e.g. on every app launch) without duplicating or throwing.
// replaceIfExists: true (default) restarts an existing region so updated
// position/notification settings take effect.
bool alreadyExisted = await geofenceManager.TryStartMonitoring(region);
```

### Distance Calculations

```csharp
var toronto = new Position(43.6532, -79.3832);
var newYork = new Position(40.7128, -74.0060);

var distance = toronto.GetDistanceTo(newYork);
Console.WriteLine($"{distance.TotalKilometers} km");
Console.WriteLine($"{distance.TotalMiles} miles");
Console.WriteLine($"{distance.TotalMeters} meters");

// Check if a position is inside a geofence region
var region = new GeofenceRegion("test", toronto, Distance.FromKilometers(1));
bool inside = region.IsPositionInside(new Position(43.6540, -79.3840));

// Check if current GPS position is inside a region
bool? isInside = await gpsManager.IsInsideRegion(
    toronto,
    Distance.FromKilometers(1)
);
```

### Motion Activity Recognition

```csharp
public class MyViewModel
{
    readonly IMotionActivityManager activityManager;

    public MyViewModel(IMotionActivityManager activityManager)
    {
        this.activityManager = activityManager;
    }

    public async Task StartTracking()
    {
        var access = await this.activityManager.RequestAccess();
        if (access != AccessState.Available)
        {
            // Handle denied/restricted
            return;
        }

        await this.activityManager.StartListener();
        this.activityManager.MotionActivityReadingReceived += this.OnActivityReadingReceived;
    }

    void OnActivityReadingReceived(object? sender, MotionActivityReading reading)
    {
        var activity = reading.Activity;      // Walking, Running, Cycling, etc.
        var confidence = reading.Confidence;  // Low, Medium, High
        var timestamp = reading.Timestamp;
    }

    public async Task StopTracking()
    {
        this.activityManager.MotionActivityReadingReceived -= this.OnActivityReadingReceived;
        await this.activityManager.StopListener();
    }
}
```

### Motion Activity Background Delegate

```csharp
public class MyMotionActivityDelegate : IMotionActivityDelegate
{
    readonly ILogger<MyMotionActivityDelegate> logger;

    public MyMotionActivityDelegate(ILogger<MyMotionActivityDelegate> logger)
    {
        this.logger = logger;
    }

    public Task OnReading(MotionActivityReading reading)
    {
        this.logger.LogInformation(
            "Motion Activity: {Activity}, Confidence={Confidence}",
            reading.Activity,
            reading.Confidence
        );
        return Task.CompletedTask;
    }
}

// Registration
services.AddMotionActivity<MyMotionActivityDelegate>();
```

### Background GPS with Realtime Mode

```csharp
var request = GpsRequest.Realtime(requestPreciseAccuracy: true);
var access = await gpsManager.RequestAccess(request);

if (access == AccessState.Available)
{
    await gpsManager.StartListener(request);
}
```

---

## Troubleshooting

### GPS listener does not start

- Ensure `AddGps()` or `AddGps<T>()` is called in `MauiProgram.cs`.
- Verify `RequestAccess` returns `AccessState.Available` before calling `StartListener`.
- On Android, ensure Google Play Services are available. If not, Shiny falls back to the legacy location API automatically.

### Geofence events not firing

- Ensure `AddGeofencing<T>()` is called in `MauiProgram.cs`.
- Verify `RequestAccess` returns `AccessState.Available`.
- On iOS, the system limits the number of monitored regions to 20. Check `GetMonitorRegions()` count.
- On Android without Google Play Services, geofencing falls back to GPS-direct mode automatically.

### Permission denied on iOS

- Add `NSLocationWhenInUseUsageDescription` to `Info.plist`.
- For background location, also add `NSLocationAlwaysAndWhenInUseUsageDescription`.
- For background GPS modes (`Standard` or `Realtime`), enable the "Location updates" background mode in Xcode capabilities.

### Permission denied on Android

- Add to `AndroidManifest.xml`:
  ```xml
  <uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
  <uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
  <uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />
  ```
- For `GpsBackgroundMode.Realtime` on Android, a foreground service notification is required.

### Battery drain with geofencing

- Prefer `AddGeofencing<T>()` over `AddGpsDirectGeofencing<T>()`. The GPS-direct approach uses realtime GPS which is battery intensive.
- Use `GpsBackgroundMode.Standard` instead of `Realtime` when possible. Standard mode provides 3-4 updates per hour on Android and uses significant location changes on iOS.

### Distance or Position throws on construction

- `Position` validates that latitude is between -90 and 90, and longitude is between -180 and 180. Values outside these ranges throw `ArgumentException`.
- `Distance` is constructed from total kilometers. Use factory methods like `Distance.FromMeters()` or `Distance.FromMiles()` for clarity.
