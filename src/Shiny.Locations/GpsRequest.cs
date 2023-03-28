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


public record GpsRequest(    

    /// <summary>
    /// Sets the background mode - null means "don't run in background"
    /// </summary>
    GpsBackgroundMode BackgroundMode = GpsBackgroundMode.None,

    /// <summary>
    /// The desired accuracy of the GPS lock
    /// </summary>
    GpsAccuracy Accuracy = GpsAccuracy.Normal,

    /// <summary>
    /// Distance filter in meters
    /// </summary>
    double DistanceFilterMeters = 0
)
{
    public static GpsRequest Realtime(bool background, double distanceFilterMeters = 0) => new(
        background
            ? GpsBackgroundMode.Realtime
            : GpsBackgroundMode.None,

        GpsAccuracy.Highest,

        distanceFilterMeters
    );

    public static GpsRequest Foreground => new(GpsBackgroundMode.None, GpsAccuracy.Normal);


    /// <summary>
    /// Sets if the location should be precise or approximate b
    /// </summary>
    //public bool Precise { get; set; } = true;


    ///// <summary>
    ///// This is the desired interval - the OS does not guarantee this time - it come sooner or later
    ///// </summary>
    //public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);


    ///// <summary>
    ///// This is a guaranteed throttle - updates cannot come faster than this value - this value MUST be lower than your interval
    ///// </summary>
    //public TimeSpan? ThrottledInterval { get; set; }


    /// <summary>
    /// The minimum distance travelled before firing event
    /// </summary>
    //public Distance? MinimumDistance { get; set; }
}
