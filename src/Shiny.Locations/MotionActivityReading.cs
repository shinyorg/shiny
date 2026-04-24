using System;

namespace Shiny.Locations;


public record MotionActivityReading(
    MotionActivityType Activity,
    MotionActivityConfidence Confidence,
    DateTimeOffset Timestamp
);
