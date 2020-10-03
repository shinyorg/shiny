using System;
using Tizen.Location;

namespace Shiny.Locations
{
    public class GpsReading : IGpsReading
    {
        readonly Location location;
        public GpsReading(Location location)
            => this.location = location;


        /// <inheritdoc />
        public double Altitude => this.location.Altitude;

        /// <inheritdoc />
        public double Heading => this.location.Direction;

        /// <inheritdoc />
        public double HeadingAccuracy => this.location.Accuracy;

        /// <inheritdoc />
        public double Speed => this.location.Speed;

        /// <inheritdoc />
        public double SpeedAccuracy => -1;

        /// <inheritdoc />
        public Position Position => new Position(this.location.Latitude, this.location.Longitude);

        /// <inheritdoc />
        public double PositionAccuracy => this.location.Accuracy;

        /// <inheritdoc />
        public DateTime Timestamp => this.location.Timestamp;
    }
}
