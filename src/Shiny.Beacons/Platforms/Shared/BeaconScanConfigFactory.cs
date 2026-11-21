using Shiny.BluetoothLE;

namespace Shiny.Beacons;


/// <summary>
/// Chooses the platform scan options used for ranging and for background monitoring.
/// </summary>
static class BeaconScanConfigFactory
{
    /// <summary>
    /// Builds the scan configuration for the requested duty cycle.
    /// </summary>
    /// <param name="forMonitoring">
    /// True for background monitoring, which trades latency for battery; false for foreground
    /// ranging, which wants every advertisement it can get.
    /// </param>
    /// <param name="options">Supplies the scan filter (<see cref="BeaconRangingOptions.ServiceUuids"/>).</param>
    public static ScanConfig Create(bool forMonitoring, BeaconRangingOptions options)
    {
#if ANDROID
        // Ranging averages RSSI over a window, so it wants the highest advertisement rate the radio
        // will give - the pre-revival module ranged on Balanced and got a fraction of the samples.
        // Monitoring only needs to notice presence within the exit timeout, so it runs LowPower.
        // Batching is off either way: batched results arrive with stale, coalesced RSSI values.
        var config = new AndroidScanConfig(
            forMonitoring
                ? Android.Bluetooth.LE.ScanMode.LowPower
                : Android.Bluetooth.LE.ScanMode.LowLatency,
            UseScanBatching: false,
            ServiceUuids: options.ServiceUuids
        );

        // Android delivers nothing from an unfiltered scan while the screen is off. An iBeacon has no
        // service UUID, so without one from the app the scan filters on Apple's iBeacon prefix - one
        // filter for every region, so adding a region never restarts the (throttled) scan.
        if (options.ServiceUuids.Length == 0 && !options.AllowNonAppleCompanyId)
        {
            config = config with
            {
                ManufacturerData = [new AndroidManufacturerDataFilter(
                    IBeaconPacket.AppleCompanyId,
                    [IBeaconPacket.BeaconType, IBeaconPacket.BeaconLength]
                )]
            };
        }
        return config;
#else
        return new ScanConfig(options.ServiceUuids);
#endif
    }


    /// <summary>
    /// Builds the scan configuration for Eddystone, which can be filtered down to its service UUID.
    /// </summary>
    /// <param name="forMonitoring">See <see cref="Create"/>.</param>
    /// <remarks>
    /// Filtering on 0xFEAA is what lets Eddystone scanning keep running when an iOS app is
    /// backgrounded - CoreBluetooth delivers nothing in the background from an unfiltered scan.
    /// </remarks>
    public static ScanConfig CreateEddystone(bool forMonitoring)
    {
#if ANDROID
        return new AndroidScanConfig(
            forMonitoring
                ? Android.Bluetooth.LE.ScanMode.LowPower
                : Android.Bluetooth.LE.ScanMode.LowLatency,
            UseScanBatching: false,
            IncludeExtendedAdvertisements: true,
            ServiceUuids: EddystoneParser.ServiceUuid
        );
#else
        return new ScanConfig(EddystoneParser.ServiceUuid);
#endif
    }
}
