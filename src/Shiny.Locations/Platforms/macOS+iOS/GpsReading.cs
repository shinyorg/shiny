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

        /// <inheritdoc />
        public double Altitude => this.location.Altitude;

        /// <inheritdoc />
        public double Heading => this.location.Course;

        /// <inheritdoc />
        public double HeadingAccuracy => this.location.VerticalAccuracy;

        /// <inheritdoc />
        public double Speed => this.location.Speed;

        /// <inheritdoc />
        public double SpeedAccuracy => this.location.SpeedAccuracy;

        /// <inheritdoc />
        public Position Position { get; }

        /// <inheritdoc />
        public double PositionAccuracy => this.location.HorizontalAccuracy;

        /// <inheritdoc />
        public DateTime Timestamp => (DateTime)this.location.Timestamp;
    }
}
