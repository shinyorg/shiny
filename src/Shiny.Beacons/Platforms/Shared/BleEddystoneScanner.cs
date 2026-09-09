using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Shiny.Beacons.Infrastructure;
using Shiny.BluetoothLE;

namespace Shiny.Beacons;


/// <summary>
/// Scans for Eddystone frames on every platform, Apple included.
/// </summary>
/// <remarks>
/// Eddystone is service data under UUID 0xFEAA rather than manufacturer data, so CoreBluetooth
/// passes it through untouched - unlike iBeacon. The scan filters on that service UUID, which also
/// happens to be what iOS requires before it will deliver scan results to a backgrounded app.
/// </remarks>
public class BleEddystoneScanner : IEddystoneScanner
{
    // see BleBeaconScanner - the filter dictionary needs sweeping on a long-running scan
    const int PruneEvery = 500;

    readonly IBleManager bleManager;
    readonly BeaconRangingOptions options;
    readonly Lazy<IObservable<EddystoneFrame>> scanner;
    int sinceLastPrune;


    /// <summary>
    /// Creates the scanner.
    /// </summary>
    /// <param name="bleManager">The BLE central used to scan.</param>
    /// <param name="options">Filtering, distance and threshold settings.</param>
    public BleEddystoneScanner(IBleManager bleManager, BeaconRangingOptions options)
    {
        this.bleManager = bleManager;
        this.options = options;
        this.scanner = new Lazy<IObservable<EddystoneFrame>>(() =>
        {
            var filters = new RssiFilterSet(options);

            return this.bleManager
                .Scan(BeaconScanConfigFactory.CreateEddystone(false))
                .Select(result => this.ToFrame(result, filters))
                .Where(x => x != null)
                .Select(x => x!)
                .Finally(filters.Clear)
                .Publish()
                .RefCount();
        });
    }


    /// <inheritdoc />
    public AccessState CurrentStatus => this.bleManager.CurrentAccess;

    /// <inheritdoc />
    public Task<AccessState> RequestAccess() => this.bleManager.RequestAccess().ToTask();


    /// <inheritdoc />
    public IObservable<EddystoneFrame> WhenFrameReceived()
        => Observable
            .FromAsync(this.RequestAccess)
            .Do(access => access.Assert())
            .SelectMany(_ => this.scanner.Value);


    EddystoneFrame? ToFrame(ScanResult result, RssiFilterSet filters)
    {
        var serviceData = result.AdvertisementData?.ServiceData;
        if (serviceData == null || serviceData.Length == 0)
            return null;

        var eddystone = serviceData.FirstOrDefault(x => EddystoneParser.IsEddystoneServiceUuid(x.Uuid));
        if (eddystone?.Data == null || eddystone.Data.Length == 0)
            return null;

        var peripheralId = result.Peripheral.Uuid;
        var filtered = filters.Add(peripheralId, result.Rssi);

        if (++this.sinceLastPrune >= PruneEvery)
        {
            this.sinceLastPrune = 0;
            filters.PruneIdle();
        }

        return EddystoneParser.Parse(eddystone.Data, peripheralId, result.Rssi, this.options, filtered);
    }
}
