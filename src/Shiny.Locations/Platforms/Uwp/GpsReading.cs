using System;
using Windows.Devices.Geolocation;

namespace Shiny.Locations
{
    public class GpsReading : IGpsReading
    {
        readonly Geocoordinate coordinate;


        public GpsReading(Geocoordinate coordinate) 
        { 
            this.coordinate = coordinate;
            this.Position = new Position(
                coordinate.Point.Position.Latitude, 
                coordinate.Point.Position.Longitude
            );
        }


        public double Altitude => this.coordinate.Point.Position.Altitude;
        public double Heading => this.coordinate.Heading ?? -1;
        public double HeadingAccuracy => this.coordinate.Accuracy;
        public double Speed => this.coordinate.Speed ?? 0;
        public Position Position { get; }
        public double PositionAccuracy => this.coordinate.Accuracy;
        public DateTime Timestamp => this.coordinate.Timestamp.DateTime;
    }
}
