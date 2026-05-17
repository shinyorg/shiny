using System;
using System.Collections.Generic;

namespace Shiny.BluetoothLE;


/// <summary>
/// Central manager for discovering, connecting to, and tracking BLE peripherals.
/// </summary>
public interface IBleManager
{
    /// <summary>
    /// Gets the current BLE adapter access state without prompting the user.
    /// </summary>
    AccessState CurrentAccess { get; }

    /// <summary>
    /// Requests the runtime permissions and adapter access required to use BluetoothLE.
    /// </summary>
    /// <returns>An observable that emits the resulting access state.</returns>
    IObservable<AccessState> RequestAccess();

    /// <summary>
    /// Returns a previously seen peripheral by its identifier, or null if it has not been observed in this session.
    /// </summary>
    /// <param name="peripheralUuid">The platform-specific peripheral identifier.</param>
    /// <returns>The known peripheral instance, or null when not found.</returns>
    IPeripheral? GetKnownPeripheral(string peripheralUuid);

    /// <summary>
    /// Gets a value indicating whether a scan is currently active.
    /// </summary>
    bool IsScanning { get; }

    /// <summary>
    /// Stops the current scan. Use this only when the original scan subscription is no longer reachable.
    /// </summary>
    void StopScan();

    /// <summary>
    /// Gets the peripherals this app currently has a live connection with.
    /// </summary>
    /// <returns>The currently connected peripherals.</returns>
    IEnumerable<IPeripheral> GetConnectedPeripherals();

    /// <summary>
    /// Starts scanning for advertising BluetoothLE peripherals. Only one scan can be active at a time;
    /// dispose the returned subscription to stop scanning.
    /// </summary>
    /// <param name="scanConfig">Optional filter and platform-specific scan options.</param>
    /// <returns>An observable that emits each advertisement received during the scan.</returns>
    IObservable<ScanResult> Scan(ScanConfig? scanConfig = null);
}

//IObservable<IPeripheral> WhenPeripheralStatusChanged();
//IObservable<AccessState> WhenStatusChanged()
// this really needs to be handled by a delegate because that's where the status changes, user should still call RequestAccess or trap when it fails starting a scan operation
