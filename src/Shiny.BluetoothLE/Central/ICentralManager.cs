using System;
using System.Collections.Generic;


namespace Shiny.BluetoothLE.Central
{
    public interface ICentralManager
    {
        /// <summary>
        /// The detected name of the peripheral
        /// </summary>
        string AdapterName { get; }

        /// <summary>
        /// This readonly property contains a flags enum stating what platform adapter features that are available
        /// </summary>
        BleFeatures Features { get; }

        /// <summary>
        /// Requests/ensures appropriate platform permissions where necessary
        /// </summary>
        /// <returns></returns>
        IObservable<AccessState> RequestAccess();

        /// <summary>
        /// Get a known peripheral
        /// </summary>
        /// <param name="deviceId">Peripheral identifier.</param>
        IObservable<IPeripheral> GetKnownPeripheral(Guid deviceId);

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
        IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(Guid? serviceUuid = null);

        /// <summary>
        /// Get the list of paired peripherals
        /// </summary>
        /// <returns></returns>
        IObservable<IEnumerable<IPeripheral>> GetPairedPeripherals();

        /// <summary>
        /// Start scanning for BluetoothLE peripherals
        /// WARNING: only one scan can be active at a time.  Use IsScanning to check for active scanning
        /// </summary>
        /// <returns></returns>
        IObservable<IScanResult> Scan(ScanConfig config = null);

        /// <summary>
        /// Monitor for status changes with adapter (on/off/permissions)
        /// </summary>
        /// <returns></returns>
        IObservable<AccessState> WhenStatusChanged();

        /// <summary>
        /// Opens the platform settings screen
        /// </summary>
        void OpenSettings();

        /// <summary>
        /// Toggles the bluetooth adapter on/off - returns true if successful
        /// Works only on Android
        /// </summary>
        /// <returns></returns>
        void SetAdapterState(bool enable);


        //void RegisterBackgroundScan<T>(Guid serviceUuid) where T : IBlePeripheralAdvertisedDelegate;
        //void RegisterDeviceRestore<T>(Guid peripheralUuid) where T : IBlePeripheralConnectedDelegate;
    }
}