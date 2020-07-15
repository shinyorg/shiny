using System;


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

        /// <summary>
        /// The geofence region unique identifier.
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// The center of the region.
        /// </summary>
        public Position Center { get; }

        /// <summary>
        /// The radius of the region.
        /// </summary>
        public Distance Radius { get; }

        /// <summary>
        /// Determines if this region is single use.
        /// </summary>
        public bool SingleUse { get; set; }

        /// <summary>
        /// Determines if the region should notify when entered.
        /// </summary>
        public bool NotifyOnEntry { get; set; } = true;

        /// <summary>
        /// Determines if the region should notify when exited.
        /// </summary>
        public bool NotifyOnExit { get; set; } = true;

        public override string ToString() => $"[Identifier: {this.Identifier}]";
        public bool Equals(GeofenceRegion other) => (this.Identifier) == (other?.Identifier);
        public override bool Equals(object obj) => obj is GeofenceRegion region && this.Equals(region);
        public override int GetHashCode() => (this.Identifier)?.GetHashCode() ?? 0;
        public static bool operator ==(GeofenceRegion left, GeofenceRegion right) => Equals(left, right);
        public static bool operator !=(GeofenceRegion left, GeofenceRegion right) => !Equals(left, right);
    }
}
