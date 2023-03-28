using System;

namespace Shiny.Locations;

/// <summary>
/// Check documentation at https://developers.google.com/android/reference/com/google/android/gms/location/LocationRequest
/// </summary>
/// <param name="BackgroundMode"></param>
/// <param name="Accuracy"></param>
/// <param name="DistanceFilter"></param>
/// <param name="Interval"></param>
/// <param name="FastestIntervalMillis"></param>
public record AndroidGpsRequest(
    GpsBackgroundMode BackgroundMode = GpsBackgroundMode.None,
    GpsAccuracy Accuracy = GpsAccuracy.Normal,
    double DistanceFilter = 0,
    int IntervalMillis = 0,
    int FastestIntervalMillis = 10000, // 10 seconds
    bool WaitForAccurateLocation = false
) : GpsRequest(
    BackgroundMode,
    Accuracy,
    DistanceFilter
);