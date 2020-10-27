using System;


namespace Shiny.Bluetooth
{
    public interface IBluetoothScanner
    {
        // TODO: scanresult with rssi & service uuids - as well as filters
        // TODO: may need EAprotocol filter?  Other?
        IObservable<IBluetoothDevice> Scan();

        /// <summary>
        /// Get current scanning status
        /// </summary>
        bool IsScanning { get; }
    }
}
