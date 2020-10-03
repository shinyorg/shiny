using System;
using Shiny.Locations;


namespace Shiny.Testing.Locations
{
    public class GpsReading : IGpsReading
    {
        /// <inheritdoc />
        public double Altitude { get; set; }

        /// <inheritdoc />
        public double Heading { get; set; }

        /// <inheritdoc />
        public double HeadingAccuracy { get; set; }

        /// <inheritdoc />
        public double Speed { get; set; }

        /// <inheritdoc />
        public double SpeedAccuracy { get; set; }

        /// <inheritdoc />
        public Position Position { get; set; }

        /// <inheritdoc />
        public double PositionAccuracy { get; set; }

        /// <inheritdoc />
        public DateTime Timestamp { get; set; }


        public static GpsReading Create(double lat, double lng) => new GpsReading
        {
            Position = new Position(lat, lng)
        };
    }
}
