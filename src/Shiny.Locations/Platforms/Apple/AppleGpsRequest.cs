using CoreLocation;

namespace Shiny.Locations;


public record AppleGpsRequest(
    GpsBackgroundMode BackgroundMode = GpsBackgroundMode.None,
    double DistanceFilterMeters = 0,
    bool ShowsBackgroundLocationIndicator = true,
    bool PausesLocationUpdatesAutomatically = false,
    bool UseSignificantLocationChanges = false,
    CLActivityType ActivityType = CLActivityType.Other
) : GpsRequest(
    BackgroundMode
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