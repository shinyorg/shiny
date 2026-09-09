using System;
using System.Reactive.Linq;
using Shiny.Beacons.Infrastructure;
using Shiny.BluetoothLE;

namespace Shiny.Beacons;


/// <summary>
/// Turns a raw BLE scan into a stream of iBeacon observations with filtered distances.
/// </summary>
/// <remarks>
/// <para>
/// This is the engine behind ranging and monitoring on every platform except Apple's. iBeacon is
/// manufacturer data under Apple's company identifier, which Android, Windows, BlueZ and Web
/// Bluetooth all hand over untouched - only CoreBluetooth strips it, which is why Apple platforms
/// go through CoreLocation instead.
/// </para>
/// <para>
/// The scan is shared: one underlying <see cref="IBleManager.Scan"/> subscription feeds every
/// caller, because the platforms allow only one scan at a time.
/// </para>
/// </remarks>
public class BleBeaconScanner
{
    // A monitoring scan can run for days in a busy environment, and every distinct beacon identity
    // it has ever heard owns a filter. The samples inside each one age out on their own, but the
    // dictionary itself does not - so empty filters are swept periodically rather than never.
    const int PruneEvery = 500;

    readonly IBleManager bleManager;
    readonly BeaconRangingOptions options;
    readonly RssiFilterSet filters;
    readonly IObservable<Beacon> scanner;
    int sinceLastPrune;


    /// <summary>
    /// Creates a scanner.
    /// </summary>
    /// <param name="bleManager">The BLE central used to scan.</param>
    /// <param name="options">Filtering, distance and threshold settings.</param>
    /// <param name="scanConfig">
    /// Platform scan options. Android passes an <c>AndroidScanConfig</c> here to select a scan mode.
    /// </param>
    public BleBeaconScanner(IBleManager bleManager, BeaconRangingOptions options, ScanConfig? scanConfig = null)
    {
        this.bleManager = bleManager;
        this.options = options;
        this.filters = new RssiFilterSet(options);

        this.scanner = this.bleManager
            .Scan(scanConfig)
            .Select(this.ToBeacon)
            .Where(x => x != null)
            .Select(x => x!)
            .Finally(() => this.filters.Clear())
            .Publish()
            .RefCount();
    }


    /// <summary>
    /// Every iBeacon observation seen while subscribed.
    /// </summary>
    public IObservable<Beacon> WhenBeaconSeen() => this.scanner;


    /// <summary>
    /// Only the observations belonging to a region.
    /// </summary>
    /// <param name="region">The region to filter to.</param>
    public IObservable<Beacon> WhenBeaconSeen(BeaconRegion region)
        => this.scanner.Where(region.IsBeaconInRegion);


    Beacon? ToBeacon(ScanResult result)
    {
        var manufacturerData = result.AdvertisementData?.ManufacturerData;
        if (manufacturerData?.Data == null)
            return null;

        var isBeacon = this.options.AllowNonAppleCompanyId
            ? IBeaconPacket.IsIBeacon(manufacturerData.Data)
            : IBeaconPacket.IsIBeacon(manufacturerData.CompanyId, manufacturerData.Data);

        if (!isBeacon)
            return null;

        var parsed = IBeaconPacket.Read(manufacturerData.Data);
        if (parsed == null)
            return null;

        var (uuid, major, minor, advertisedTxPower) = parsed.Value;
        var identity = new BeaconIdentity(uuid, major, minor);

        // a beacon that calibrates itself at 0 dBm is not calibrated at all
        var txPower = advertisedTxPower == 0 ? this.options.DefaultTxPower : advertisedTxPower;

        var filtered = this.filters.Add(identity.ToString(), result.Rssi);
        var distance = this.options.DistanceEstimator.Estimate(filtered, txPower);

        if (++this.sinceLastPrune >= PruneEvery)
        {
            this.sinceLastPrune = 0;
            this.filters.PruneIdle();
        }

        return new Beacon(
            uuid,
            major,
            minor,
            result.Rssi,
            this.options.ToProximity(distance),
            distance,
            advertisedTxPower,
            this.options.TimeProvider.GetUtcNow()
        );
    }
}
