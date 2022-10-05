using System;

namespace Shiny.Sensors;


public record CompassReading(
    CompassAccuracy Accuracy,
    double MagneticHeading,
    double? TrueHeading
)
{
    public override string ToString() => $"Magnetic: {this.MagneticHeading} - True: {this.TrueHeading} - Accuracy: {this.Accuracy}";
}