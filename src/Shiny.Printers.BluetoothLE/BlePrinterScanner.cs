using System.Reactive.Linq;
using Shiny.BluetoothLE;

namespace Shiny.Printers.BluetoothLE;


/// <summary>Scans for nearby BLE thermal printers.</summary>
public interface IPrinterScanner
{
    /// <summary>
    /// Requests BLE access then scans, emitting a <see cref="DiscoveredPrinter"/> for each recognised
    /// printer. The scan runs until the subscription is disposed. The same printer may be emitted repeatedly.
    /// </summary>
    IObservable<DiscoveredPrinter> Scan();
}


/// <summary>Default <see cref="IPrinterScanner"/> filtering on the <see cref="KnownPrinterProfiles"/> service UUIDs.</summary>
public sealed class BlePrinterScanner(IBleManager bleManager) : IPrinterScanner
{
    readonly IBleManager bleManager = bleManager ?? throw new ArgumentNullException(nameof(bleManager));


    public IObservable<DiscoveredPrinter> Scan()
        => Observable
            .FromAsync(() => this.bleManager.RequestAccessAsync())
            .SelectMany(access => access == AccessState.Available
                ? this.bleManager.Scan(new ScanConfig(KnownPrinterProfiles.ServiceUuids))
                : Observable.Throw<ScanResult>(new InvalidOperationException($"Bluetooth access is not available ({access}).")))
            .Select(Map)
            .Where(printer => printer is not null)
            .Select(printer => printer!);


    static DiscoveredPrinter? Map(ScanResult result)
    {
        // The scan is filtered on known service UUIDs, but the advertisement may not echo which one
        // matched. Prefer an exact match; otherwise fall back to the generic ESC/POS profile.
        var profile = KnownPrinterProfiles.Match(result.AdvertisementData.ServiceUuids)
            ?? KnownPrinterProfiles.All.FirstOrDefault();

        if (profile is null)
            return null;

        var name = result.Peripheral.Name ?? result.AdvertisementData.LocalName;
        return new DiscoveredPrinter(result.Peripheral, name, result.Rssi, profile.Config);
    }
}
