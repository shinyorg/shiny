using System;


namespace Shiny.TripTracker
{
    public class Trip
    {
        public Guid Id { get; set; }
        public Distance? TotalDistance { get; set; }

        public DateTimeOffset DateStarted { get; set; }

        public Position StartPosition { get; set; }
        
        public Position? EndPosition { get; set; }
        public DateTimeOffset? DateFinished { get; set; }
    }
}
