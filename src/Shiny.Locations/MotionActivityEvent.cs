using System;

namespace Shiny.Locations;


public class MotionActivityEvent
{
    public MotionActivityEvent(MotionActivityType types, MotionActivityConfidence confidence, DateTimeOffset timestamp)
    {
        this.Types = types;
        this.Confidence = confidence;
        this.Timestamp = timestamp;
    }

    /// <summary>
    /// The motion activity type.
    /// </summary>
    public MotionActivityType Types { get; }

    /// <summary>
    /// The confidence of the accuracy.
    /// </summary>
    public MotionActivityConfidence Confidence { get; }

    /// <summary>
    /// The time the motion happened.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
