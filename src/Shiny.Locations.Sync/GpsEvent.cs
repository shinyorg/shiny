using System;


namespace Shiny.Locations.Sync
{
    public class GpsEvent : LocationEvent
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double Heading { get; set; }
        public double HeadingAccuracy { get; set; }
        public double Speed { get; set; }
        public double PositionAccuracy { get; set; }
    }
}
