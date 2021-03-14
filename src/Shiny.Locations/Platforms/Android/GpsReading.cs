using System;
using Android.Locations;


namespace Shiny.Locations
{
    class GpsReading : IGpsReading
    {
        readonly Location location;
        DateTime? time;

        public GpsReading(Location location)
        {
            this.location = location;
            this.Position = new Position(location.Latitude, location.Longitude);
        }

        /// <inheritdoc />
        public double Altitude => this.location.Altitude;

        /// <inheritdoc />
        public double Heading => this.location.Bearing;

        /// <inheritdoc />
        public double HeadingAccuracy => this.location.BearingAccuracyDegrees;

        /// <inheritdoc />
        public double Speed => this.location.Speed;

        /// <inheritdoc />
        public double SpeedAccuracy => this.location.SpeedAccuracyMetersPerSecond;

        /// <inheritdoc />
        public Position Position { get; }

        /// <inheritdoc />
        public double PositionAccuracy => this.location.Accuracy;


        /// <inheritdoc />
        public DateTime Timestamp => this.time ??= DateTimeOffset.FromUnixTimeMilliseconds(this.location.Time).UtcDateTime;
    }
}
