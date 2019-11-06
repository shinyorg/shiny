﻿using System;


namespace Shiny.Locations
{
    [Flags]
    public enum MotionActivityType
    {
        Unknown = 0,
        Stationary = 1,
        Walking = 2,
        Running = 4,
        Automotive = 8,
        Cycling = 16
    }


    public enum MotionActivityConfidence
    {
        Low,
        Medium,
        High
    }

    public class MotionActivityEvent
    {
        public MotionActivityEvent(MotionActivityType types, MotionActivityConfidence confidence, DateTimeOffset timestamp)
        {
            this.Types = types;
            this.Confidence = confidence;
            this.Timestamp = timestamp;
        }

        public MotionActivityType Types { get; }
        public MotionActivityConfidence Confidence { get; }
        public DateTimeOffset Timestamp { get; }
    }
}
