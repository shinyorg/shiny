using System;


namespace Shiny.Beacons
{
    public class BeaconRegion : IEquatable<BeaconRegion>
    {
        public BeaconRegion(string identifier, Guid uuid, ushort? major = 0, ushort? minor = 0)
        {
            this.Identifier = identifier;
            this.Uuid = uuid;
            if (major > 0)
                this.Major = major;

            if (minor > 0)
                this.Minor = minor;
        }


        public string Identifier { get; }
        public Guid Uuid { get; }
        public ushort? Major { get; }
        public ushort? Minor { get; }
        public bool NotifyOnEntry { get; set; } = true;
        public bool NotifyOnExit { get; set; } = true;


        public override string ToString() => $"[Identifier: {this.Identifier} - UUID: {this.Uuid} - Major: {this.Major ?? 0} - Minor: {this.Minor ?? 0}]";
        public bool Equals(BeaconRegion other) => (this.Identifier, this.Uuid, this.Major, this.Minor).Equals((other?.Identifier, other?.Uuid, other?.Major, other?.Minor));
        public static bool operator ==(BeaconRegion left, BeaconRegion right) => Equals(left, right);
        public static bool operator !=(BeaconRegion left, BeaconRegion right) => !Equals(left, right);
        public override bool Equals(object obj) => obj is BeaconRegion region && this.Equals(region);
        public override int GetHashCode() => (this.Identifier, this.Uuid, this.Major, this.Minor).GetHashCode();
    }
}