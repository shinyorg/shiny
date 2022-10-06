using System;

namespace Shiny.Sensors;

public record MotionReading(
    double X,
    double Y,
    double Z
)
{
    public override string ToString() => $"X: {this.X} - Y: {this.Y} - Z: {this.Z}";
}