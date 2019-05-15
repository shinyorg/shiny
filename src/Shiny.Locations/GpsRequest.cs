using System;


namespace Shiny.Locations
{
    public class GpsRequest
    {
        public bool UseBackground { get; set; }
        public double? DeferredDistanceMeters { get; set; }
        public TimeSpan? DeferredTime { get; set; }
        public GpsPriority Priority { get; set; } = GpsPriority.Normal;
    }
}
