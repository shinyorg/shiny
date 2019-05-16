using System;


namespace Shiny.Locations
{
    public class GpsRequest
    {
        public bool UseBackground { get; set; } = true;
        public Distance DeferredDistance { get; set; }
        public TimeSpan? DeferredTime { get; set; }
        public GpsPriority Priority { get; set; } = GpsPriority.Normal;
    }
}
