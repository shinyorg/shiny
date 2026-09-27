using System;

namespace Shiny.Locations;


/// <summary>
/// A single motion activity reading reported by the device, describing the user's current activity.
/// </summary>
/// <param name="Activity">The detected motion activity type.</param>
/// <param name="Confidence">How confident the platform is in the detected activity.</param>
/// <param name="Timestamp">The instant at which the reading was captured.</param>
public record MotionActivityReading(
    MotionActivityType Activity,
    MotionActivityConfidence Confidence,
    DateTimeOffset Timestamp
);
