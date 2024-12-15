using System;

namespace Shiny.Health;

public enum Interval
{
    Minutes,
    Hours,
    Days
}

public enum DataType
{
    StepCount,
    HeartRate,
    Calories,
    Distance
}

public abstract record HealthResult(
    DataType Type,
    DateTimeOffset Start,
    DateTimeOffset End
);

public record NumericHealthResult(
    DataType DataType,
    DateTimeOffset Start,
    DateTimeOffset End,
    double Value
) : HealthResult(DataType, Start, End);
