using System;
using Shiny.Locations;


namespace Shiny.Testing.Locations
{
    public class GpsReading : IGpsReading
    {
        public double Altitude { get; set; }
        public double Heading { get; set; }
        public double HeadingAccuracy { get; set; }
        public double Speed { get; set; }
        public Position Position { get; set; }
        public double PositionAccuracy { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
