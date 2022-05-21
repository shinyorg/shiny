using System;

namespace Shiny.Locations;

public enum GpsBackgroundMode
{
    None,
    Standard,
    Realtime
}


public enum GeofenceState
{
    Unknown = 0,
    Entered = 1,
    Exited = 2
}

[Flags]
public enum MotionActivityType
{
    Unknown = 0,
    Stationary = 1,
    Walking = 2,
    Running = 4,
    Automotive = 8,
    Cycling = 16
}


public enum MotionActivityConfidence
{
    Low,
    Medium,
    High
}

// helps with auto pausing on iOS
//public enum GpsActivityType
//{
//    Airborne,
//    Automotive,
//    Fitness,
//    Other
//}

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
