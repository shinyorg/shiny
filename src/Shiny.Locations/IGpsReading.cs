using System;


namespace Shiny.Locations
{
    public interface IGpsReading
    {
        double Altitude { get; }
        double Heading { get; }
        double HeadingAccuracy { get; }
        double Speed { get; }
        Position Position { get; }
        double PositionAccuracy { get; }
        DateTime Timestamp { get; }
    }
}
