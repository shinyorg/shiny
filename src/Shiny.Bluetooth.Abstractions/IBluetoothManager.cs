using System;


namespace Shiny.Bluetooth
{
    public interface IBluetoothManager
    {
        /// <summary>
        /// Requests user permissions (if not answered already) and checks device status
        /// </summary>
        /// <returns></returns>
        IObservable<AccessState> RequestAccess();

        /// <summary>
        /// Returns a list of connected devices
        /// </summary>
        /// <returns></returns>
        IObservable<IBluetoothDevice> GetConnectedDevices();

        /// <summary>
        /// Current Feature/Adapter Status
        /// </summary>
        AccessState Status { get; }
    }
}
