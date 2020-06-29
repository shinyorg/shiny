using System;
using SQLite;


namespace Shiny.TripTracker
{
    public class Trip
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }
        public double TotalDistanceMeters { get; set; }

        public DateTimeOffset DateStarted { get; set; }
        public double StartLatitude { get; set; }
        public double StartLongitude { get; set; }
        
        public double? EndLatitude { get; set; }
        public double? EndLongitude { get; set; }
        public DateTimeOffset? DateFinished { get; set; }
    }
}
