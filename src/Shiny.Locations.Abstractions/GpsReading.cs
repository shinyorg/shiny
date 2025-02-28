using System;

namespace Shiny.Locations;


public record GpsReading(
    /// <summary>
    /// The position of the reading.
    /// </summary>
    Position Position,

    /// <summary>
    /// The position accuracy.
    /// </summary>
    double PositionAccuracy,

    /// <summary>
    /// The time stamp.
    /// </summary>
    DateTimeOffset Timestamp,

    /// <summary>
    /// The heading of the reading.
    /// </summary>
    double Heading,

    /// <summary>
    /// The accuracy of the heading.
    /// </summary>
    double HeadingAccuracy,

    /// <summary>
    /// The altitude of the reading.
    /// </summary>
    double Altitude,

    /// <summary>
    /// The current speed.
    /// </summary>
    double Speed,

    /// <summary>
    /// The accuracy in meters per second for the speed
    /// </summary>
    double SpeedAccuracy
);
