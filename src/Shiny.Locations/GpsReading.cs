using System;

namespace Shiny.Locations;


/// <summary>
/// A single GPS reading captured by the device, including position, accuracy, heading, altitude, and speed.
/// </summary>
/// <param name="Position">The latitude and longitude of the reading.</param>
/// <param name="PositionAccuracy">The horizontal accuracy of the position, in meters.</param>
/// <param name="Timestamp">The instant at which the reading was captured.</param>
/// <param name="Heading">The compass heading at the time of the reading, in degrees.</param>
/// <param name="HeadingAccuracy">The accuracy of the heading, in degrees.</param>
/// <param name="Altitude">The altitude of the reading, in meters above sea level.</param>
/// <param name="Speed">The speed of the device at the time of the reading, in meters per second.</param>
/// <param name="SpeedAccuracy">The accuracy of the speed value, in meters per second.</param>
/// <param name="Floor">The floor of the building the device is on, if available; otherwise zero.</param>
/// <param name="IsStationary">Indicates whether the device was detected as stationary when the reading was taken.</param>
public record GpsReading(
    Position Position,
    double PositionAccuracy,
    DateTimeOffset Timestamp,
    double Heading,
    double HeadingAccuracy,
    double Altitude,
    double Speed,
    double SpeedAccuracy,
    int Floor = 0,
    bool IsStationary = false
);
