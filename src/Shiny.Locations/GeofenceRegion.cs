using System;
using System.Collections.Generic;


namespace Shiny.Locations
{
    public class GeofenceRegion : IEquatable<GeofenceRegion>
    {
        public GeofenceRegion(string identifier,
                              Position center,
                              Distance radius)
        {
            this.Identifier = identifier;
            this.Center = center;
            this.Radius = radius;
        }


        public string Identifier { get; }
        public Position Center { get; }
        public Distance Radius { get; }

        public bool SingleUse { get; set; }
        public bool NotifyOnEntry { get; set; } = true;
        public bool NotifyOnExit { get; set; } = true;

        public Dictionary<string, object>? Payload { get; set; }

        public override string ToString() => $"[Identifier: {this.Identifier}]";
        public bool Equals(GeofenceRegion other) => (this.Identifier) == (other?.Identifier);
        public override bool Equals(object obj) => obj is GeofenceRegion region && this.Equals(region);
        public override int GetHashCode() => (this.Identifier)?.GetHashCode() ?? 0;

        public static bool operator ==(GeofenceRegion left, GeofenceRegion right) => Equals(left, right);
        public static bool operator !=(GeofenceRegion left, GeofenceRegion right) => !Equals(left, right);
    }
}
