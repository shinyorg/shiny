using CoreLocation;

namespace Shiny.Locations;


public record AppleGpsRequest(
    GpsBackgroundMode BackgroundMode = GpsBackgroundMode.None,
    GpsAccuracy Accuracy = GpsAccuracy.Normal,
    double DistanceFilter = 0,
    bool ShowsBackgroundLocationIndicator = true,
    bool PausesLocationUpdatesAutomatically = false,
    bool UseSignificantLocationChanges = false,
    CLActivityType? ActivityType = null
) : GpsRequest(
    BackgroundMode,
    Accuracy,
    DistanceFilter
);