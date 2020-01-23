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
        public List<PayloadEntry>? Payload { get; set; }

        public struct PayloadEntry
        {
            public string? Key { get; set; }
            public object? Value { get; set; }

            public static explicit operator PayloadEntry(KeyValuePair<string?, object?> kvp) =>
                new PayloadEntry { Key = kvp.Key, Value = kvp.Value };
        }
    }
}
