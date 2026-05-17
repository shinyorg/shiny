namespace Shiny.Locations;


/// <summary>
/// Describes how the GPS listener should behave when the app is not in the foreground.
/// </summary>
public enum GpsBackgroundMode
{
    /// <summary>
    /// The GPS listener runs only while the app is in the foreground.
    /// </summary>
    None,

    /// <summary>
    /// Low-power background tracking. iOS uses significant location changes; Android delivers roughly three to four updates per hour.
    /// </summary>
    Standard,

    /// <summary>
    /// Continuous, high-frequency background tracking. iOS uses full background updates and Android runs a foreground service, both delivering updates approximately every second.
    /// </summary>
    Realtime
}

/// <summary>
/// Configuration for starting a GPS listener, including background behavior and accuracy preferences.
/// </summary>
/// <param name="BackgroundMode">How the listener should behave when the app is backgrounded.</param>
/// <param name="RequestPreciseAccuracy">Whether to request high-precision location instead of an approximate one.</param>
/// <param name="AutoRestart">Whether the listener should automatically restart after the app is force-quit and relaunched by the OS.</param>
public record GpsRequest(
    GpsBackgroundMode BackgroundMode = GpsBackgroundMode.None,
    bool RequestPreciseAccuracy = false,
    bool AutoRestart = true
)
{
    /// <summary>
    /// A foreground-only GPS request with default accuracy.
    /// </summary>
    public static GpsRequest Foreground => new(GpsBackgroundMode.None, false);

    /// <summary>
    /// A low-power background GPS request with default accuracy.
    /// </summary>
    public static GpsRequest Background => new(GpsBackgroundMode.Standard, false);

    /// <summary>
    /// A high-frequency real-time background GPS request.
    /// </summary>
    /// <param name="requestPreciseAccuracy">Whether to request high-precision location.</param>
    public static GpsRequest Realtime(bool requestPreciseAccuracy) => new(GpsBackgroundMode.Realtime, requestPreciseAccuracy);
}
