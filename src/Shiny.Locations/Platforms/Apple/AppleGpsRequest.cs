using CoreLocation;

namespace Shiny.Locations;


public record AppleGpsRequest(
    GpsBackgroundMode BackgroundMode = GpsBackgroundMode.None,
    bool RequestPreciseAccuracy = false,
    bool AutoRestart = true,
    double DistanceFilterMeters = 0,
    bool ShowsBackgroundLocationIndicator = true,
    bool PausesLocationUpdatesAutomatically = false,
    bool UseSignificantLocationChanges = false,
    CLActivityType ActivityType = CLActivityType.Other,
    int StationaryMetersThreshold = 10,
    int StationarySecondsThreshold = 30
) : GpsRequest(
    BackgroundMode,
    RequestPreciseAccuracy,
    AutoRestart
);

public static class AppleGpsRequestExtensions
{
    public static AppleGpsRequest ToApple(this GpsRequest request)
    {
        if (request is AppleGpsRequest apple)
            return apple;

        return new AppleGpsRequest(
            request.BackgroundMode
        );
    }
}