using System;


namespace Shiny.BluetoothLE
{
    public interface IPeripheral
    {
        /// <summary>
        /// The peripheral name - note that this is not readable in the background on most platforms
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The peripheral UUID - note that this will not be the same per platform
        /// </summary>
        string Uuid { get; }

        /// <summary>
        /// The current connection status
        /// </summary>
        /// <value>The status.</value>
        ConnectionState Status { get; }

        /// <summary>
        /// Connect to a peripheral
        /// </summary>
        /// <param name="config">Connection configuration</param>
        void Connect(ConnectionConfig? config = null);

        /// <summary>
        /// Disconnect from the peripheral and cancel persistent connection
        /// </summary>
        void CancelConnection();

        /// <summary>
        /// This fires when a peripheral connection fails
        /// </summary>
        /// <returns></returns>
        IObservable<BleException> WhenConnectionFailed();

        /// <summary>
        /// Monitor connection status
        /// </summary>
        /// <returns></returns>
        IObservable<ConnectionState> WhenStatusChanged();

        /// <summary>
        /// BLE service discovery - This method does not complete.  It will clear all discovered services on subsequent connections
        /// and does not require a connection to hook to it.
        /// </summary>
        IObservable<IGattService> DiscoverServices();

        /// <summary>
        /// Searches for a known service
        /// </summary>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        IObservable<IGattService> GetKnownService(string serviceUuid);

        /// <summary>
        /// Monitor peripheral name changes
        /// </summary>
        /// <returns></returns>
        IObservable<string> WhenNameUpdated();

        /// <summary>
        /// Reads the RSSI of the connected peripheral
        /// </summary>
        /// <returns></returns>
        IObservable<int> ReadRssi();

        /// <summary>
        /// This is the current MTU size (must be connected to get a true value)
        /// </summary>
        int MtuSize { get; }
    }
}