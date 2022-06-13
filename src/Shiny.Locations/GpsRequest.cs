using System;


namespace Shiny.Locations
{
    public class GpsRequest
    {
        public static GpsRequest Realtime(bool background) => new GpsRequest
        {
            Accuracy = GpsAccuracy.Highest,
            BackgroundMode = background
                ? GpsBackgroundMode.Realtime
                : GpsBackgroundMode.None
        };

        public static GpsRequest Foreground => new GpsRequest
        {
            Accuracy = GpsAccuracy.Normal
        };


        /// <summary>
        /// Sets if the location should be precise or approximate b
        /// </summary>
        //public bool Precise { get; set; } = true;


        /// <summary>
        /// Sets the background mode - null means "don't run in background"
        /// </summary>
        public GpsBackgroundMode BackgroundMode { get; set; } = GpsBackgroundMode.None;


        ///// <summary>
        ///// This is the desired interval - the OS does not guarantee this time - it come sooner or later
        ///// </summary>
        //public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);


        ///// <summary>
        ///// This is a guaranteed throttle - updates cannot come faster than this value - this value MUST be lower than your interval
        ///// </summary>
        //public TimeSpan? ThrottledInterval { get; set; }


        /// <summary>
        /// The desired Priority/Accuracy of the GPS lock
        /// </summary>
        public GpsAccuracy Accuracy { get; set; } = GpsAccuracy.Normal;


        /// <summary>
        /// The minimum distance travelled before firing event
        /// </summary>
        //public Distance? MinimumDistance { get; set; }
    }
}
