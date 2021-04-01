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

        public static GpsRequest Foreground => new GpsRequest
        {
            Priority = GpsPriority.Normal,
            UseBackground = false
        };

        /// <summary>
        /// Determines if background updates should occur
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


        /// <summary>
        /// The minimum distance travelled before firing event
        /// </summary>
        public Distance? MinimumDistance { get; set; }
    }
}
