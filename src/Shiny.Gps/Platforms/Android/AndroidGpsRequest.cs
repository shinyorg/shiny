using System;
using Android.Gms.Location;

namespace Shiny.Locations;

/// <summary>
/// Check documentation at https://developers.google.com/android/reference/com/google/android/gms/location/LocationRequest
/// </summary>
/// <param name="BackgroundMode">
/// None - will shut down if app goes to the background
/// Standard - will use the standard background durations from the platform (roughly 4-5 pings per hour)
/// Realtime - will launch an android foreground service that will ensure continuous flow of GPS pings
/// </param>
/// <param name="GpsPriority"></param>
/// <param name="DistanceFilterMeters"></param>
/// <param name="IntervalMillis"></param>
/// <param name="WaitForAccurateLocation"></param>
/// <param name="StopForegroundServiceWithTask">Will shutdown the foreground service (if applicable) if the app is swiped away</param>
public record AndroidGpsRequest(
    GpsBackgroundMode BackgroundMode = GpsBackgroundMode.None,
    GpsPriority GpsPriority = GpsPriority.Balanced,

    // TODO: classic location settings vs fused
    double DistanceFilterMeters = 0,
    int IntervalMillis = 1000,
    bool WaitForAccurateLocation = false,
    bool StopForegroundServiceWithTask = false,
    bool RequestPreciseAccuracy = false,
    bool AutoRestart = true,
    int StationaryMetersThreshold = 10,
    int StationarySecondsThreshold = 30
) : GpsRequest(
    BackgroundMode,
    RequestPreciseAccuracy,
    AutoRestart
);

public enum GpsPriority : int
{
    Balanced = 102,
    HighAccuracy = 100,
    LowPower = 104,
    Passive = 105
    // [Register("PRIORITY_BALANCED_POWER_ACCURACY")]
    // public const int PriorityBalancedPowerAccuracy = 102;
    // [Register("PRIORITY_HIGH_ACCURACY")]
    // public const int PriorityHighAccuracy = 100;
    // [Register("PRIORITY_LOW_POWER")]
    // public const int PriorityLowPower = 104;
    // [Register("PRIORITY_PASSIVE")]
    // public const int PriorityPassive = 105;
}