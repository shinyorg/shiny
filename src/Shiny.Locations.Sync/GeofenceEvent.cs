using System;


namespace Shiny.Locations.Sync
{
    public class GeofenceEvent : LocationEvent
    {
        public string Identifier { get; set; }
        public bool Entered { get; set; }
    }
}
