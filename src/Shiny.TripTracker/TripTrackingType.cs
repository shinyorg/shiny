using System;


namespace Shiny.TripTracker
{
    public enum TripTrackingType
    {
        Stationary,

        Running,
        Walking,
        Cycling,

        OnFoot = Walking | Running,
        Exercise = Walking | Running | Cycling,

        Automotive
    }
}
