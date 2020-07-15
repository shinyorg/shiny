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
        public double Speed => throw new NotImplementedException();

        /// <inheritdoc />
        public Position Position => throw new NotImplementedException();

        /// <inheritdoc />
        public double PositionAccuracy => throw new NotImplementedException();

        /// <inheritdoc />
        public DateTime Timestamp => this.location.Timestamp;
    }
}
