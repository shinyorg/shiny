namespace Shiny.Locations;


public enum GpsBackgroundMode
{
    /// <summary>
    /// No background mode
    /// </summary>
    None,
    
    /// <summary>
    /// iOS: Significant Location Changes
    /// Android: BACKGROUND - receive 3-4 updates per hour 
    /// </summary>
    Standard,
    
    /// <summary>
    /// iOS: Full background request - Updates every 1 second
    /// Android: Foreground Service - Updates every 1 second
    /// </summary>
    Realtime
}

/// <summary>
/// GPS runtime request 
/// </summary>
/// <param name="BackgroundMode"></param>
/// <param name="RequestPreciseAccuracy">Try to use precise accuracy</param>
/// <param name="AutoRestart">This will auto restart the service if your app is swiped away and restarts</param>
public record GpsRequest(    
    GpsBackgroundMode BackgroundMode = GpsBackgroundMode.None,
    bool RequestPreciseAccuracy = false,
    bool AutoRestart = true
)
{
    public static GpsRequest Foreground => new(GpsBackgroundMode.None, false);
    public static GpsRequest Background => new(GpsBackgroundMode.None, false);
    public static GpsRequest Realtime(bool requestPreciseAccuracy) => new(GpsBackgroundMode.Realtime, requestPreciseAccuracy);
}
