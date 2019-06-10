using System;
using System.Linq;
using System.Reactive.Linq;
#if WINDOWS_UWP || __ANDROID__
using Shiny.BluetoothLE.Central;
#endif


namespace Shiny.Beacons
{
    public static class Extensions
    {
#if WINDOWS_UWP || __ANDROID__
        public static IObservable<Beacon> ScanForBeacons(this ICentralManager centralManager, bool forMonitoring = false) => centralManager
            .Scan(new ScanConfig
            {
                //AndroidUseScanBatching = true,
                ScanType = forMonitoring
                    ? BleScanType.LowPowered
                    : BleScanType.Balanced
            })
            .Where(x => x.IsBeacon())
            .Select(x => x.AdvertisementData.ManufacturerData.Data.Parse(x.Rssi));


        public static bool IsBeacon(this IScanResult result)
        {
            var md = result.AdvertisementData?.ManufacturerData;

            if (md == null || md.Data == null || md.Data.Length != 23)
                return false;

            if (md.CompanyId != 76)
                return false;

            return md.Data.IsBeaconPacket();
        }

#endif

        public static bool IsBeaconInRegion(this BeaconRegion region, Beacon beacon)
        {
            if (!region.Uuid.Equals(beacon.Uuid))
                return false;

            if (region.Major == null && region.Minor == null)
                return true;

            if (region.Major != beacon.Major)
                return false;

            if (region.Minor == null || region.Minor == beacon.Minor)
                return true;

            return false;
        }


        /// <summary>
        /// This should not be called by iOS as txpower is not available in the advertisement packet
        /// </summary>
        /// <returns>The beacon.</returns>
        /// <param name="data">Data.</param>
        /// <param name="rssi">Rssi.</param>
        public static Beacon Parse(this byte[] data, int rssi)
        {
            //var uuidString = BitConverter.ToString(data, 4, 16).Replace("-", String.Empty);
            //var uuid = new Guid(uuidString);
            //var major = BitConverter.ToUInt16(data.Skip(20).Take(2).Reverse().ToArray(), 0);
            //var minor = BitConverter.ToUInt16(data.Skip(22).Take(2).Reverse().ToArray(), 0);
            //var txpower = data[24];
            //var accuracy = CalculateAccuracy(txpower, rssi);
            //var proximity = CalculateProximity(accuracy);

            //return new Beacon(uuid, major, minor, accuracy, proximity);
            //Console.WriteLine("Beacon Packet: " + data.ToHex());

            if (BitConverter.IsLittleEndian)
            {
                var uuidString = BitConverter.ToString(data, 2, 16).Replace("-", String.Empty);
                var uuid = new Guid(uuidString);
                var major = BitConverter.ToUInt16(data.Skip(18).Take(2).Reverse().ToArray(), 0);
                var minor = BitConverter.ToUInt16(data.Skip(20).Take(2).Reverse().ToArray(), 0);
                var txpower = data[22];
                //var accuracy = CalculateAccuracy(txpower, rssi);
                //var proximity = CalculateProximity(accuracy);
                var proximity = CalculateProximity(txpower, rssi);

                return new Beacon(uuid, major, minor, proximity);
            }
            throw new ArgumentException("TODO");
        }


        //public static byte[] ToIBeaconPacket(this Beacon beacon, int txpower)
        //{
        //    using (var ms = new MemoryStream())
        //    {
        //        using (var br = new BinaryWriter(ms))
        //        {
        //            br.Write(76);
        //            br.Write(new byte[] { 0, 0, 0 });
        //            br.Write(ToBytes(beacon.Uuid));
        //            br.Write(BitConverter.GetBytes(beacon.Major).Reverse().ToArray());
        //            br.Write(BitConverter.GetBytes(beacon.Minor).Reverse().ToArray());
        //            br.Write(txpower);
        //        }

        //        return ms.ToArray();
        //    }
        //}

        // 6 meters+ = far
        // 2 meters+ = near
        // 0.5 meters += immediate

        public static Proximity CalculateProximity(int txpower, double rssi)
        {
            var distance = Math.Pow(10d, (txpower * -1 - rssi) / 20);
            Console.WriteLine("Distance: " + distance);
            //if (accuracy < 0)
            //    return Proximity.Unknown;

            //if (accuracy < 0.5)
            //    return Proximity.Immediate;

            //if (accuracy <= 4.0)
            //    return Proximity.Near;

            //return Proximity.Far;
            if (distance >= 6E-6d)
                return Proximity.Far;

            if (distance > 0.5E-6d)
                return Proximity.Near;

            return Proximity.Immediate;
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
