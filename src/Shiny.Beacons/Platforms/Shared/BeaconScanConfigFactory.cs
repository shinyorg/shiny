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
    /// <param name="options">The ranging options - <see cref="BeaconRangingOptions.AllowNonAppleCompanyId"/> decides the filter.</param>
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
            UseScanBatching: false
        );

        // Android delivers nothing from an unfiltered scan while the screen is off, so a monitored
        // region went silent and then "exited" every time the phone was locked.
        var filter = GetIBeaconFilter(options);
        if (filter != null)
            config = config with { ManufacturerData = [new(filter.Value.CompanyId, filter.Value.Prefix)] };

        return config;
#else
        return new ScanConfig();
#endif
    }


    /// <summary>
    /// The manufacturer data an iBeacon scan is filtered on: Apple's company identifier and the
    /// 0x02 0x15 prefix every iBeacon payload starts with.
    /// </summary>
    /// <returns>
    /// Null when <see cref="BeaconRangingOptions.AllowNonAppleCompanyId"/> is on - those payloads
    /// arrive under other company identifiers, so an Apple filter would drop them and the scan has
    /// to stay unfiltered.
    /// </returns>
    internal static (ushort CompanyId, byte[] Prefix)? GetIBeaconFilter(BeaconRangingOptions options)
        => options.AllowNonAppleCompanyId
            ? null
            : (IBeaconPacket.AppleCompanyId, [IBeaconPacket.BeaconType, IBeaconPacket.BeaconLength]);


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
