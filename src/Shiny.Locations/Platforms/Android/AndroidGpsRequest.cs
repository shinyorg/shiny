using System;

namespace Shiny.Locations;

/// <summary>
/// Check documentation at https://developers.google.com/android/reference/com/google/android/gms/location/LocationRequest
/// </summary>
/// <param name="BackgroundMode">
/// None - will shut down if app goes to the background
/// Standard - will use the standard background durations from the platform (roughly 4-5 pings per hour)
/// Realtime - will launch an android foreground service that will ensure continuous flow of GPS pings
/// </param>
/// <param name="Accuracy"></param>
/// <param name="DistanceFilterMeters"></param>
/// <param name="IntervalMillis"></param>
/// <param name="WaitForAccurateLocation"></param>
/// <param name="StopForegroundServiceWithTask">Will shutdown the foreground service (if applicable) if the app is swiped away</param>
public record AndroidGpsRequest(
    GpsBackgroundMode BackgroundMode = GpsBackgroundMode.None,
    GpsAccuracy Accuracy = GpsAccuracy.Normal,
    double DistanceFilterMeters = 0,
    int IntervalMillis = 0,
    bool WaitForAccurateLocation = false,
    bool StopForegroundServiceWithTask = false
) : GpsRequest(
    BackgroundMode,
    Accuracy,
    DistanceFilterMeters
);