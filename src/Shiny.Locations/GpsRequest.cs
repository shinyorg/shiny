using System;


namespace Shiny.Locations
{
    public class GpsRequest
    {
        /// <summary>
        /// Determines if background updates should occur - used mostly by iOS
        /// </summary>
        public bool UseBackground { get; set; } = true;


        /// <summary>
        /// This will defer an update until the minimum distance has been moved
        /// </summary>
        public Distance DeferredDistance { get; set; }


        /// <summary>
        /// This will defer a location request for a set period of time - this is not a precision science on iOS until your app is in the background
        /// </summary>
        public TimeSpan? DeferredTime { get; set; }


        /// <summary>
        /// The desired Priority/Accuracy of the GPS lock
        /// </summary>
        public GpsPriority Priority { get; set; } = GpsPriority.Normal;
    }
}
