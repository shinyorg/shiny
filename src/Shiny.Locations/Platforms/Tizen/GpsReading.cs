using System;
using Tizen.Location;

namespace Shiny.Locations
{
    public class GpsReading : IGpsReading
    {
        readonly Location location;
        public GpsReading(Location location)
            => this.location = location;


        public double Altitude => this.location.Altitude;
        public double Heading => this.location.Direction;
        public double HeadingAccuracy => this.location.Accuracy;
        public double Speed => throw new NotImplementedException();
        public Position Position => throw new NotImplementedException();
        public double PositionAccuracy => throw new NotImplementedException();
        public DateTime Timestamp => this.location.Timestamp;
    }
}
