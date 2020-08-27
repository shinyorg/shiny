using System;


namespace Shiny.TripTracker
{
    [Flags]
    public enum TripTrackType
    {
        Stationary = 1,
        Walking = 2,
        Running = 4,
        Cycling = 8,
        Automotive = 16
    }


    public class TripTrackConfig
    {
        public TripTrackType Type { get; set; }

        /// <summary>
        /// If you are tracking Stationary & Walking, current trip started as walking, but walking is no longer visible and stationary is - old trip ends, new trip starts
        /// </summary>
        public bool StartNewWhenNotOverlapping { get; }
    }
}
