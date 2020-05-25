using System;


namespace Shiny.Locations.Sync
{
    public class GeofenceEvent
    {
        public string Id { get; set; }
        public string Identifier { get; set; }
        public bool Entered { get; set; }
        public DateTimeOffset DateCreated { get; set; }
    }
}
