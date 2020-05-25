using System;


namespace Shiny.Locations.Sync
{
    public class GpsEvent
    {
        public string Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double Heading { get; set; }
        public double HeadingAccuracy { get; set; }
        public double Speed { get; set; }
        public double PositionAccuracy { get; set; }
        public DateTimeOffset DateCreated { get; set; }
    }
}
