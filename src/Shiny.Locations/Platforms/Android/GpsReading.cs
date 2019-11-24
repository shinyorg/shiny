using System;
using Android.Locations;

namespace Shiny.Locations
{
    public class GpsReading : IGpsReading
    {
        readonly Location location;


        public GpsReading(Location location)
        {
            this.location = location;
            this.Position = new Position(location.Latitude, location.Longitude);
        }


        public double Altitude => this.location.Altitude;
        public double Heading => this.location.Bearing;
        public double HeadingAccuracy => this.location.BearingAccuracyDegrees;
        public double Speed => this.location.Speed;
        public Position Position { get; }
        public double PositionAccuracy => this.location.Accuracy;


        DateTime? time;
        public DateTime Timestamp => this.time ??= DateTimeOffset.FromUnixTimeMilliseconds(this.location.Time).UtcDateTime;
    }
}
