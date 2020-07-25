using System;


namespace Shiny.Locations.Sync
{
    public abstract class LocationEvent
    {
        public string Id { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public MotionActivityType? Activities { get; set; }
    }
}
