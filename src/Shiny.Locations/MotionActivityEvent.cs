using System;
using System.Collections.Generic;

namespace Shiny.Locations;


public enum MotionActivityType
{
    Stationary = 1,
    Walking = 2,
    Running = 3,
    Automotive = 4,
    Cycling = 5
}


public enum MotionActivityConfidence
{
    Low,
    Medium,
    High
}


public record DetectedMotionActivity(
    MotionActivityType ActivityType,
    MotionActivityConfidence Confidence
);


public record MotionActivityEvent(
    IList<DetectedMotionActivity> DetectedActivities,
    DetectedMotionActivity? MostProbableActivity,
    DateTimeOffset Timestamp
)
{
    public bool IsUnknown => this.DetectedActivities.Count == 0;
}