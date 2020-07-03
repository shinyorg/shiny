using System;
using SQLite;


namespace Shiny.Locations.Sync
{
    public abstract class LocationEvent
    {
        [PrimaryKey]
        public string Id { get; set; }
        public DateTimeOffset DateCreated { get; set; }
        public MotionActivityEvent? Activities { get; set; }
    }
}
