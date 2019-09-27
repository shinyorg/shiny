using System;
using System.Reactive.Linq;
using Shiny.BluetoothLE.Central;


namespace Shiny.Beacons
{
    public static class CentralManagerExtensions
    {
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
    }
}
