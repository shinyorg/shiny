using System;


namespace Shiny.Locations
{
    public struct MotionActivityTimeBlock
    {
        public MotionActivityTimeBlock(MotionActivityType type, DateTimeOffset start, DateTimeOffset end)
        {
            this.Type = type;
            this.Start = start;
            this.End = end;
        }


        public MotionActivityType Type { get; }
        public DateTimeOffset Start { get; }
        public DateTimeOffset End { get; }
    }
}
