using System;
using System.Reactive.Linq;

namespace Shiny.BluetoothLE;


public static class BleManagerExtensions
{
    /// <summary>
    /// This will scan until the peripheral a specific peripheral is found, then cancel the scan
    /// </summary>
    /// <param name="bleManager"></param>
    /// <param name="peripheralName"></param>
    /// <returns></returns>
    public static IObservable<IPeripheral> ScanUntilPeripheralFound(this IBleManager bleManager, string peripheralName) => bleManager
        .Scan()
        .Where(scanResult =>
        {
            if (scanResult.Peripheral.Name?.Equals(peripheralName, StringComparison.InvariantCultureIgnoreCase) ?? false)
                return true;

            if (scanResult.AdvertisementData?.LocalName?.Equals(peripheralName, StringComparison.CurrentCultureIgnoreCase) ?? false)
                return true;

            return false;
        })
        .Take(1)
        .Select(x => x.Peripheral);


    /// <summary>
    /// This will scan until the first peripheral with a specified service UUID is found
    /// </summary>
    /// <param name="bleManager"></param>
    /// <param name="serviceUuid"></param>
    /// <returns></returns>
    public static IObservable<IPeripheral> ScanUntilFirstPeripheralFound(this IBleManager bleManager, string serviceUuid) => bleManager
        .Scan(new(serviceUuid))
        .Take(1)
        .Select(x => x.Peripheral);


    /// <summary>
    /// Scans only for distinct peripherals instead of repeating each peripheral scan response - this will only give you peripherals, not RSSI or ad packets
    /// </summary>
    /// <param name="bleManager"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IObservable<IPeripheral> ScanForUniquePeripherals(this IBleManager bleManager, ScanConfig? config = null) => bleManager
        .Scan(config)
        .Distinct(x => x.Peripheral.Uuid)
        .Select(x => x.Peripheral);
}
