namespace Shiny.Locations;


public enum GpsBackgroundMode
{
    None,
    Standard,
    Realtime
}


public enum GpsAccuracy
{
    //Reduced,

    /// <summary>
    /// 3km
    /// </summary>
    Lowest = 1,

    /// <summary>
    /// 1km
    /// </summary>
    Low = 2,

    /// <summary>
    /// 100 meters
    /// </summary>
    // 100 meters
    Normal = 3,

    /// <summary>
    /// 10 meters
    /// </summary>
    High = 4,

    /// <summary>
    /// Immediate results
    /// </summary>
    Highest = 5
}


/// <summary>
/// Distance filter in meters
/// </summary>
public record GpsRequest(    
    GpsBackgroundMode BackgroundMode = GpsBackgroundMode.None,
    GpsAccuracy Accuracy = GpsAccuracy.Normal,
    double DistanceFilterMeters = 0,
    bool RequestFineLocation = true
)
{
    public static GpsRequest Realtime(bool background, double distanceFilterMeters = 0, bool requestFineLocation = true) => new(
        background
            ? GpsBackgroundMode.Realtime
            : GpsBackgroundMode.None,

        GpsAccuracy.Highest,
        distanceFilterMeters,
        requestFineLocation
    );

    public static GpsRequest Foreground => new(GpsBackgroundMode.None, GpsAccuracy.Normal);


    ///// <summary>
    ///// This is the desired interval - the OS does not guarantee this time - it come sooner or later
    ///// </summary>
    //public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);


    ///// <summary>
    ///// This is a guaranteed throttle - updates cannot come faster than this value - this value MUST be lower than your interval
    ///// </summary>
    //public TimeSpan? ThrottledInterval { get; set; }


    // /// <summary>
    // /// The minimum distance travelled before firing event
    // /// </summary>
    //public Distance? MinimumDistance { get; set; }
}
