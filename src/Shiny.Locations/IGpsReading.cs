using System;


namespace Shiny.Locations
{
    public interface IGpsReading
    {
        /// <summary>
        /// The altitude of the reading.
        /// </summary>
        double Altitude { get; }

        /// <summary>
        /// The heading of the reading.
        /// </summary>
        double Heading { get; }

        /// <summary>
        /// The accuracy of the heading.
        /// </summary>
        double HeadingAccuracy { get; }

        /// <summary>
        /// The current speed.
        /// </summary>
        double Speed { get; }

        /// <summary>
        /// The accuracy in meters per second for the speed
        /// </summary>
        double SpeedAccuracy { get; }

        /// <summary>
        /// The position of the reading.
        /// </summary>
        Position Position { get; }

        /// <summary>
        /// The position accuracy.
        /// </summary>
        double PositionAccuracy { get; }

        /// <summary>
        /// The time stamp.
        /// </summary>
        DateTime Timestamp { get; }
    }
}
