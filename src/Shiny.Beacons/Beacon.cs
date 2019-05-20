using System;
using System.IO;
using System.Linq;


namespace Shiny.Beacons
{
    public class Beacon : IEquatable<Beacon>
    {
        public Beacon(Guid uuid, ushort major, ushort minor, double accuracy, Proximity proximity)
        {
            this.Uuid = uuid;
            this.Major = major;
            this.Minor = minor;
            this.Accuracy = accuracy;
            this.Proximity = proximity;
        }


        public Guid Uuid { get; }
        public ushort Minor { get; }
        public ushort Major { get; }
        public double Accuracy { get; }
        public Proximity Proximity { get; }


        public override string ToString() => $"[Beacon: Uuid={this.Uuid}, Major={this.Major}, Minor={this.Minor}]";
        public bool Equals(Beacon other) => (this.Uuid, this.Major, this.Minor) == (other?.Uuid, other?.Major, other?.Minor);
        public static bool operator ==(Beacon left, Beacon right) => Equals(left, right);
        public static bool operator !=(Beacon left, Beacon right) => !Equals(left, right);
        public override bool Equals(object obj) => obj is Beacon beacon && this.Equals(beacon);
        public override int GetHashCode() => (this.Uuid, this.Major, this.Minor).GetHashCode();


        /// <summary>
        /// This should not be called by iOS as txpower is not available in the advertisement packet
        /// </summary>
        /// <returns>The beacon.</returns>
        /// <param name="data">Data.</param>
        /// <param name="rssi">Rssi.</param>
        public static Beacon Parse(byte[] data, int rssi)
        {
            var uuidString = BitConverter.ToString(data, 4, 16).Replace("-", String.Empty);
            var uuid = new Guid(uuidString);
            var major = BitConverter.ToUInt16(data.Skip(20).Take(2).Reverse().ToArray(), 0);
            var minor = BitConverter.ToUInt16(data.Skip(22).Take(2).Reverse().ToArray(), 0);
            var txpower = data[24];
            var accuracy = CalculateAccuracy(txpower, rssi);
            var proximity = CalculateProximity(accuracy);

            return new Beacon(uuid, major, minor, accuracy, proximity);
        }


        public byte[] ToIBeaconPacket()
        {
            using (var ms = new MemoryStream())
            {
                using (var br = new BinaryWriter(ms))
                {
                    br.Write(76);
                    br.Write(new byte[] { 0, 0, 0 });
                    br.Write(ToBytes(this.Uuid));
                    br.Write(BitConverter.GetBytes(this.Major).Reverse().ToArray());
                    br.Write(BitConverter.GetBytes(this.Minor).Reverse().ToArray());
                    br.Write(0); // tx power
                }
                return ms.ToArray();
            }
        }


        public static double CalculateAccuracy(int txpower, double rssi)
        {
            var ratio = rssi * 1 / txpower;
            if (ratio < 1.0)
                return Math.Pow(ratio,10);

            var accuracy =  0.89976 * Math.Pow(ratio, 7.7095) + 0.111;
            return accuracy;
        }


        public static Proximity CalculateProximity(double accuracy)
        {
            if (accuracy < 0)
                return Proximity.Unknown;

            if (accuracy < 0.5)
                return Proximity.Immediate;

            if (accuracy <= 4.0)
                return Proximity.Near;

            return Proximity.Far;
        }


        public static bool IsBeaconPacket(byte[] data, bool skipManufacturerByte = true)
        {
            if (data == null)
                return false;

            if (data.Length != 23)
                return false;

            // apple manufacturerID - https://www.bluetooth.com/specifications/assigned-numbers/company-Identifiers
            if (!skipManufacturerByte && data[0] != 76)
                return false;

            return true;
        }

        static byte[] ToBytes(Guid guid)
        {
            var hex = guid
                .ToString()
                .Replace("-", String.Empty)
                .Replace("{", String.Empty)
                .Replace("}", String.Empty)
                .Replace(":", String.Empty)
                .Replace("-", String.Empty);

            var bytes = Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();

            return bytes;
        }
    }
}