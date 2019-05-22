#if WINDOWS_UWP || __ANDROID__
using System;
using System.Linq;
using System.Reactive.Linq;
using Shiny.BluetoothLE.Central;


namespace Shiny.Beacons
{
    public static class BeaconExtensions
    {
        public static IObservable<Beacon> ScanForBeacons(this ICentralManager centralManager, bool forMonitoring = false) => centralManager
            .Scan(new ScanConfig
            {
                AndroidUseScanBatching = true,
                ScanType = forMonitoring
                    ? BleScanType.LowPowered
                    : BleScanType.Balanced
            })
            .Where(x => x.IsBeacon())
            .Select(x => x.ToBeacon());


        public static bool IsBeacon(this IScanResult result)
        {
            var md = result.AdvertisementData?.ManufacturerData;

            if (md == null)
                return false;

            if (md.CompanyId != 76)
                return false;

            return Beacon.IsBeaconPacket(md.Data);
        }


        public static Beacon ToBeacon(this IScanResult result)
        {
            var data = result.AdvertisementData.ManufacturerData.Data;
            Console.WriteLine("RAW: " + BitConverter.ToString(data));

            var uuidString = BitConverter.ToString(data, 2, 16).Replace("-", String.Empty);
            var uuid = new Guid(uuidString);
            var major = BitConverter.ToUInt16(data.Skip(18).Take(2).Reverse().ToArray(), 0);
            var minor = BitConverter.ToUInt16(data.Skip(20).Take(2).Reverse().ToArray(), 0);
            var txpower = data[22];
            var accuracy = CalculateAccuracy(txpower, result.Rssi);
            var proximity = CalculateProximity(accuracy);

            return new Beacon(uuid, major, minor, accuracy, proximity);
        }


        public static double CalculateAccuracy(int txpower, double rssi)
        {
            var ratio = rssi * 1 / Math.Abs(txpower);
            if (ratio < 1.0)
                return Math.Pow(ratio, 10);

            var accuracy = 0.89976 * Math.Pow(ratio, 7.7095) + 0.111;
            return accuracy;
        }


        public static Proximity CalculateProximity(double accuracy)
        {
            Console.WriteLine($"Accuracy: {accuracy}");
            if (accuracy >= 4.0d)
                return Proximity.Immediate;

            if (accuracy >= 0.5d)
                return Proximity.Near;

            return Proximity.Far;
        }
    }
}
#endif