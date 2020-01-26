using System.Collections.Generic;
using System.Linq;


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

        public static explicit operator GeofenceRegionStore(GeofenceRegion @this) =>
            new GeofenceRegionStore
            {
                Identifier = @this.Identifier,
                CenterLatitude = @this.Center.Latitude,
                CenterLongitude = @this.Center.Longitude,
                RadiusMeters = @this.Radius.TotalMeters,
                SingleUse = @this.SingleUse,
                NotifyOnEntry = @this.NotifyOnEntry,
                NotifyOnExit = @this.NotifyOnExit,
                Payload = @this.Payload.Cast<PayloadEntry>().ToList(),
            };

        public static explicit operator GeofenceRegion(GeofenceRegionStore store) =>
            new GeofenceRegion(
                store.Identifier,
                new Position(store.CenterLatitude, store.CenterLongitude),
                Distance.FromMeters(store.RadiusMeters)
            )
            {
                SingleUse = store.SingleUse,
                NotifyOnEntry = store.NotifyOnEntry,
                NotifyOnExit = store.NotifyOnExit,
                Payload = store.Payload.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            };

        public struct PayloadEntry
        {
            public string? Key { get; set; }
            public object? Value { get; set; }

            public static explicit operator PayloadEntry(KeyValuePair<string?, object?> kvp) =>
                new PayloadEntry { Key = kvp.Key, Value = kvp.Value };
        }
    }
}
