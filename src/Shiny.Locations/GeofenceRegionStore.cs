using System.Collections.Generic;


namespace Shiny.Locations
{
    public class GeofenceRegionStore
    {
        public string Identifier { get; set; }

        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double RadiusMeters { get; set; }

        public bool SingleUse { get; set; }
        public bool NotifyOnEntry { get; set; }
        public bool NotifyOnExit { get; set; }
        public Dictionary<string, object>? Payload { get; set; }
    }
}
