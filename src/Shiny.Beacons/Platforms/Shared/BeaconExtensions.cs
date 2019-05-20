#if WINDOWS_UWP || __ANDROID__
using System;
using System.Linq;
using System.Reactive.Linq;
using Shiny.BluetoothLE.Central;


namespace Shiny.Beacons
{
    public static class BeaconExtensions
    {
        public static IObservable<Beacon> ScanForBeacons(this ICentralManager centralManager) => centralManager
            .Scan()
            .Where(x => x.IsBeacon())
            .Select(x => x.ToBeacon());


        public static bool IsBeacon(this IScanResult result)
        {
            var md = result.AdvertisementData?.ManufacturerData;

            if (md == null)
                return false;

            if (md.CompanyId != 76)
                return false;

            if (md.Data.Length != 23)
                return false;

            return true;
        }


        public static Beacon ToBeacon(this IScanResult result)
        {
            var data = result.AdvertisementData.ManufacturerData.Data;
            Console.WriteLine("RAW: " + BitConverter.ToString(data));

            var uuidString = BitConverter.ToString(data, 2, 16).Replace("-", String.Empty);
            var uuid = new Guid(uuidString);
            var major = BitConverter.ToUInt16(data.Skip(18).Take(2).ToArray(), 0);
            var minor = BitConverter.ToUInt16(data.Skip(20).Take(2).ToArray(), 0);
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
            if (accuracy < 0)
                return Proximity.Unknown;

            if (accuracy < 0.5)
                return Proximity.Immediate;

            if (accuracy <= 4.0)
                return Proximity.Near;

            return Proximity.Far;
        }
    }
}
#endif