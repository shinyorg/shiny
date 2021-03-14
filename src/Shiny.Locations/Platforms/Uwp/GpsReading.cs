using System;
using Windows.Devices.Geolocation;


namespace Shiny.Locations
{
    class GpsReading : IGpsReading
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

        /// <inheritdoc />
        public double Altitude => this.coordinate.Point.Position.Altitude;

        /// <inheritdoc />
        public double Heading => this.coordinate.Heading ?? -1;

        /// <inheritdoc />
        public double HeadingAccuracy => this.coordinate.Accuracy;

        /// <inheritdoc />
        public double Speed => this.coordinate.Speed ?? 0;

        /// <inheritdoc />
        public double SpeedAccuracy => -1; //this.coordinate.SpeedAccuracy;

        /// <inheritdoc />
        public Position Position { get; }

        /// <inheritdoc />
        public double PositionAccuracy => this.coordinate.Accuracy;

        /// <inheritdoc />
        public DateTime Timestamp => this.coordinate.Timestamp.DateTime;
    }
}
