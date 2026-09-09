using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Shiny.BluetoothLE;

namespace Shiny.Beacons;


/// <summary>
/// Ranges iBeacons by scanning for their advertisements directly.
/// </summary>
/// <remarks>
/// Used on Android, Windows, Linux and any other host where BLE manufacturer data reaches the app.
/// Apple platforms register the CoreLocation implementation instead.
/// </remarks>
public class BleScanBeaconRangingManager : IBeaconRangingManager
{
    readonly IBleManager bleManager;
    readonly Lazy<BleBeaconScanner> scanner;


    /// <summary>
    /// Creates the manager.
    /// </summary>
    /// <param name="bleManager">The BLE central used to scan.</param>
    /// <param name="options">Filtering, distance and threshold settings.</param>
    public BleScanBeaconRangingManager(IBleManager bleManager, BeaconRangingOptions options)
    {
        this.bleManager = bleManager;
        this.scanner = new Lazy<BleBeaconScanner>(
            () => new BleBeaconScanner(bleManager, options, BeaconScanConfigFactory.Create(false))
        );
    }


    /// <inheritdoc />
    public AccessState CurrentStatus => this.bleManager.CurrentAccess;

    /// <inheritdoc />
    public Task<AccessState> RequestAccess() => this.bleManager.RequestAccess().ToTask();

    /// <inheritdoc />
    public IObservable<Beacon> WhenBeaconRanged(BeaconRegion region)
        => Observable
            .FromAsync(this.RequestAccess)
            .Do(access => access.Assert())
            .SelectMany(_ => this.scanner.Value.WhenBeaconSeen(region));
}
