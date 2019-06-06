using System;
using System.IO;
using System.Linq;


namespace Shiny.Beacons
{
    public static class Extensions
    {
        /// <summary>
        /// This should not be called by iOS as txpower is not available in the advertisement packet
        /// </summary>
        /// <returns>The beacon.</returns>
        /// <param name="data">Data.</param>
        /// <param name="rssi">Rssi.</param>
        public static Beacon Parse(this byte[] data, int rssi)
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


        public static byte[] ToIBeaconPacket(this Beacon beacon)
        {
            using (var ms = new MemoryStream())
            {
                using (var br = new BinaryWriter(ms))
                {
                    br.Write(76);
                    br.Write(new byte[] { 0, 0, 0 });
                    br.Write(ToBytes(beacon.Uuid));
                    br.Write(BitConverter.GetBytes(beacon.Major).Reverse().ToArray());
                    br.Write(BitConverter.GetBytes(beacon.Minor).Reverse().ToArray());
                    br.Write(0); // tx power
                }
                return ms.ToArray();
            }
        }


        public static double CalculateAccuracy(int txpower, double rssi)
        {
            var ratio = rssi * 1 / txpower;
            if (ratio < 1.0)
                return Math.Pow(ratio, 10);

            var accuracy = 0.89976 * Math.Pow(ratio, 7.7095) + 0.111;
            return accuracy;
        }


        public static Proximity CalculateProximity(this double accuracy)
        {
            if (accuracy < 0)
                return Proximity.Unknown;

            if (accuracy < 0.5)
                return Proximity.Immediate;

            if (accuracy <= 4.0)
                return Proximity.Near;

            return Proximity.Far;
        }


        public static bool IsBeaconPacket(this byte[] data, bool skipManufacturerByte = true)
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


        public static byte[] ToBytes(this Guid guid)
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
