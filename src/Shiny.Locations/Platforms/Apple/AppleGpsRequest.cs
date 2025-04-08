using CoreLocation;

namespace Shiny.Locations;


public record AppleGpsRequest(
    GpsBackgroundMode BackgroundMode = GpsBackgroundMode.None,
    GpsAccuracy Accuracy = GpsAccuracy.Normal,
    double DistanceFilterMeters = 0,
    bool ShowsBackgroundLocationIndicator = true,
    bool PausesLocationUpdatesAutomatically = false,
    bool UseSignificantLocationChanges = false,
    CLActivityType ActivityType = CLActivityType.Other
) : GpsRequest(
    BackgroundMode,
    Accuracy,
    DistanceFilterMeters
)
{
    public CLLiveUpdateConfiguration ModernActivityType => this.ActivityType switch
    {
        CLActivityType.Airborne => CLLiveUpdateConfiguration.Airborne,
        CLActivityType.Fitness => CLLiveUpdateConfiguration.Fitness,
        CLActivityType.AutomotiveNavigation => CLLiveUpdateConfiguration.AutomotiveNavigation,
        CLActivityType.OtherNavigation => CLLiveUpdateConfiguration.OtherNavigation,
        _ => CLLiveUpdateConfiguration.Default
    };
};

public static class AppleGpsRequestExtensions
{
    public static AppleGpsRequest ToApple(this GpsRequest request)
    {
        if (request is AppleGpsRequest apple)
            return apple;

        return new AppleGpsRequest(
            request.BackgroundMode,
            request.Accuracy,
            request.DistanceFilterMeters
        );
    }
}