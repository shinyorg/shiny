using System;
using SQLite;


namespace Shiny.TripTracker
{
    public class TripCheckin
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }
        public int TripId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public double Direction { get; set; }
        public DateTimeOffset DateCreated { get; set; }
    }
}
