using System;


namespace Shiny.BluetoothLE.Central
{
    public interface IPeripheral
    {
        //IObservable<> WriteDescriptor(Guid uuid, byte[] data);
        //IObservable<> ReadDescriptor(Guid uuid);
        //IObservable<> ReadCharacteristic(Guid uuid);
        //IObservable<> WriteCharacteristic(Guid uuid, byte[] data, bool withResponse = true)
        //IObservable<> WhenValueChanged(Guid uuid, bool useIndicationIfAvailable);

        //IObservable<string> WhenNameChanged();
        //IObservable<int> WhenRssiChanged();

        /// <summary>
        /// Returns the native peripheral instance for external use
        /// </summary>
        object NativeDevice { get; }

        /// <summary>
        /// The peripheral name - note that this is not readable in the background on most platforms
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The peripheral ID - note that this will not be the same per platform
        /// </summary>
        Guid Uuid { get; }

        /// <summary>
        /// Gets the size of the current mtu.
        /// </summary>
        int MtuSize { get; }

        /// <summary>
        /// The current pairing status
        /// </summary>
        PairingState PairingStatus { get; }

        /// <summary>
        /// The current connection status
        /// </summary>
        /// <value>The status.</value>
        ConnectionState Status { get; }

        /// <summary>
        /// Connect to a peripheral
        /// </summary>
        /// <param name="config">Connection configuration</param>
        void Connect(ConnectionConfig config = null);

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
        IObservable<IGattService> GetKnownService(Guid serviceUuid);

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
        /// Make a pairing request
        /// </summary>
        /// <returns></returns>
        IObservable<bool> PairingRequest(string pin = null);

        /// <summary>
        /// Send request to set MTU size
        /// </summary>
        /// <param name="size"></param>
        IObservable<int> RequestMtu(int size);

        /// <summary>
        /// Fires when MTU size changes
        /// </summary>
        /// <returns>The mtu change requested.</returns>
        IObservable<int> WhenMtuChanged();

        /// <summary>
        /// Begins a reliable write transaction
        /// </summary>
        /// <returns>Transaction session</returns>
        IGattReliableWriteTransaction BeginReliableWriteTransaction();
    }
}