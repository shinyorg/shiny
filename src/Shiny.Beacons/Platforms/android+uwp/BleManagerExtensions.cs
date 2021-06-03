using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Shiny.BluetoothLE;


namespace Shiny.Beacons
{
    public static class BleManagerExtensions
    {
        public static IObservable<Beacon> ScanForBeacons(this IBleManager manager, bool forMonitoring = false) => manager
            .Scan(new ScanConfig
            {
                //AndroidUseScanBatching = true,
                ScanType = forMonitoring
                    ? BleScanType.LowPowered
                    : BleScanType.Balanced
//#if MONOANDROID
//                , ServiceUuids = new List<string>
//                {

//                }
//#endif
            })
            .Where(x => x.IsBeacon())
            .Select(x => x.AdvertisementData.ManufacturerData.Data.Parse(x.Rssi));


        public static bool IsBeacon(this ScanResult result)
        {
            var md = result.AdvertisementData?.ManufacturerData;

            if (md == null || md.Data == null || md.Data.Length != 23)
                return false;

            if (md.CompanyId != 76)
                return false;

            return md.Data.IsBeaconPacket();
        }
    }
}
