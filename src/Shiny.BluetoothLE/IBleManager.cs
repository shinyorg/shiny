using System;
using System.Collections.Generic;

namespace Shiny.BluetoothLE;


public interface IBleManager
{
    /// <summary>
    /// Gets current access state
    /// </summary>
    AccessState CurrentAccess { get; }

    /// <summary>
    /// Requests necessary permissions to ensure bluetooth LE can be used
    /// </summary>
    /// <returns></returns>
    IObservable<AccessState> RequestAccess();

    /// <summary>
    /// Get a known peripheral from the last scanresult
    /// </summary>
    /// <param name="peripheralUuid">Peripheral identifier.</param>
    IPeripheral? GetKnownPeripheral(string peripheralUuid);

    /// <summary>
    /// Get current scanning status
    /// </summary>
    bool IsScanning { get; }

    /// <summary>
    /// Stop any current scan - use this if you didn't keep a disposable endpoint for Scan()
    /// </summary>
    void StopScan();

    /// <summary>
    /// Gets a list of connected peripherals by your app
    /// </summary>
    /// <returns></returns>
    IEnumerable<IPeripheral> GetConnectedPeripherals();

    /// <summary>
    /// Start scanning for BluetoothLE peripherals
    /// WARNING: only one scan can be active at a time.  Use IsScanning to check for active scanning
    /// </summary>
    /// <returns></returns>
    IObservable<ScanResult> Scan(ScanConfig? scanConfig = null);
}

//IObservable<IPeripheral> WhenPeripheralStatusChanged();
//IObservable<AccessState> WhenStatusChanged()
// this really needs to be handled by a delegate because that's where the status changes, user should still call RequestAccess or trap when it fails starting a scan operation
