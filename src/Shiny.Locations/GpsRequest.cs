using System;


namespace Shiny.Locations
{
    public class GpsRequest
    {
        public static GpsRequest Realtime(bool backgroundMode) => new GpsRequest
        {
            Priority = GpsPriority.Highest,
            Interval = TimeSpan.FromSeconds(1),
            UseBackground = backgroundMode
        };

        /// <summary>
        /// Determines if background updates should occur - used mostly by iOS
        /// </summary>
        public bool UseBackground { get; set; } = true;

        /// <summary>
        /// This is the desired interval - the OS does not guarantee this time - it come sooner or later
        /// </summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// This is a guaranteed throttle - updates cannot come faster than this value - this value MUST be lower than your interval
        /// </summary>
        public TimeSpan? ThrottledInterval { get; set; }

        /// <summary>
        /// The desired Priority/Accuracy of the GPS lock
        /// </summary>
        public GpsPriority Priority { get; set; } = GpsPriority.Normal;
    }
}
