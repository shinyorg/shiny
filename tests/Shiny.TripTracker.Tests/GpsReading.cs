using System;
using Shiny.Locations;


namespace Shiny.TripTracker.Tests
{
    public class GpsReading : IGpsReading
    {
        public static GpsReading Create(double lat, double lng) => new GpsReading
        {
            Position = new Position(lat, lng)
        };

        /// <inheritdoc />
        public double Altitude { get; set; }

        /// <inheritdoc />
        public double Heading { get; set; }

        /// <inheritdoc />
        public double HeadingAccuracy { get; set; }

        /// <inheritdoc />
        public double Speed { get; set; }

        /// <inheritdoc />
        public Position Position { get; set; }

        /// <inheritdoc />
        public double PositionAccuracy { get; set; }

        /// <inheritdoc />
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <inheritdoc />
        public double SpeedAccuracy { get; set; }
    }
}
