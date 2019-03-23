#if WINDOWS_UWP || __ANDROID__
using System;
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

            //if (md.CompanyId != 39)
            //    return false;

            return true;
            //if (data.Length < 25)
            //    return false;

            //// apple manufacturerID - https://www.bluetooth.com/specifications/assigned-numbers/company-Identifiers
            //if (data[0] != 76 || data[1] != 0)
            //    return false;

            //return true;
        }


        public static Beacon ToBeacon(this IScanResult result)
        {
            return null;
        }
    }
}
#endif