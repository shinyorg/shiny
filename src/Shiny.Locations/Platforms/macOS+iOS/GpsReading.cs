using System;
using CoreLocation;

namespace Shiny.Locations
{
    class GpsReading : IGpsReading
    {
        readonly CLLocation location;


        public GpsReading(CLLocation location)
        {
            this.location = location;
            this.Position = new Position(location.Coordinate.Latitude, location.Coordinate.Longitude);
        }


        public double Altitude => this.location.Altitude;
        public double Heading => this.location.Course;
        public double HeadingAccuracy => this.location.VerticalAccuracy;
        public double Speed => this.location.Speed;

        public Position Position { get; }
        public double PositionAccuracy => this.location.HorizontalAccuracy;
        public DateTime Timestamp => (DateTime)this.location.Timestamp;
    }
}
