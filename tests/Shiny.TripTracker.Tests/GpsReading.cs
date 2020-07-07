using System;
using Shiny.Locations;


namespace Shiny.TripTracker.Tests
{
    public class GpsReading : IGpsReading
    {
        public static GpsReading Create(double lat, double lng) => new GpsReading
        {
            Position = new Position(lat, lng)
        };


        public double Altitude { get; set; }
        public double Heading { get; set; }
        public double HeadingAccuracy { get; set; }
        public double Speed { get; set; }
        public Position Position { get; set; }
        public double PositionAccuracy { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
