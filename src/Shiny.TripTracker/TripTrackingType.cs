using System;


namespace Shiny.TripTracker
{
    public enum TripTrackingType
    {
        Stationary,

        Running,
        Walking,
        Cycling,

        // walking or running
        OnFoot,

        // walking, running, or cycling
        Exercise,

        Automotive
    }
}
