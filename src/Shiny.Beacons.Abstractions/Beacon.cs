using System;


namespace Shiny.Beacons
{
    public class Beacon : IEquatable<Beacon>
    {
        public Beacon(Guid uuid, ushort major, ushort minor, Proximity proximity, int rssi, double accuracy)
        {
            this.Uuid = uuid;
            this.Major = major;
            this.Minor = minor;
            this.Proximity = proximity;
            this.Rssi = rssi;
            this.Accuracy = accuracy;
        }


        public Guid Uuid { get; }
        public ushort Minor { get; }
        public ushort Major { get; }
        public Proximity Proximity { get; }
        public int Rssi { get; }
        public double Accuracy { get; }


        public override string ToString() => $"[Beacon: Uuid={this.Uuid}, Major={this.Major}, Minor={this.Minor}]";
        public bool Equals(Beacon other) => (this.Uuid, this.Major, this.Minor) == (other?.Uuid, other?.Major, other?.Minor);
        public static bool operator ==(Beacon left, Beacon right) => Equals(left, right);
        public static bool operator !=(Beacon left, Beacon right) => !Equals(left, right);
        public override bool Equals(object obj) => obj is Beacon beacon && this.Equals(beacon);
        public override int GetHashCode() => (this.Uuid, this.Major, this.Minor).GetHashCode();
    }
}