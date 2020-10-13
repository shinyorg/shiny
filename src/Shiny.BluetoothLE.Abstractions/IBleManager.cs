using System;
using System.Collections.Generic;


namespace Shiny.BluetoothLE
{
    public interface IBleManager
    {
        /// <summary>
        /// Requests/ensures appropriate platform permissions where necessary
        /// </summary>
        /// <returns></returns>
        IObservable<AccessState> RequestAccess();

        /// <summary>
        /// Get a known peripheral
        /// </summary>
        /// <param name="peripheralUuid">Peripheral identifier.</param>
        IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid);

        /// <summary>
        /// Returns current status of adapter (on/off/permission)
        /// </summary>
        AccessState Status { get; }

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
        /// <param name="serviceUuid">(iOS only) Service UUID filter to see peripherals that were connected outside of application</param>
        /// <returns></returns>
        IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null);

        /// <summary>
        /// Start scanning for BluetoothLE peripherals
        /// WARNING: only one scan can be active at a time.  Use IsScanning to check for active scanning
        /// </summary>
        /// <returns></returns>
        IObservable<ScanResult> Scan(ScanConfig? config = null);

        /// <summary>
        /// Monitor for status changes with adapter (on/off/permissions)
        /// </summary>
        /// <returns></returns>
        IObservable<AccessState> WhenStatusChanged();
    }
}