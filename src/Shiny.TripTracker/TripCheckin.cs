using System;


namespace Shiny.TripTracker
{
    public class TripCheckin
    {
        public Guid TripId { get; set; }
        public Position Position { get; set; }
        public double? Speed { get; set; }
        public double? Direction { get; set; }
        public DateTimeOffset DateCreated { get; set; }
    }
}
