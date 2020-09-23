using System;


namespace Shiny.Bluetooth
{
    public interface IBluetoothManager
    {
        // TODO: scanresult with rssi & service uuids - as well as filters
        IObservable<IBluetoothDevice> Scan();

        // TODO: adapter state
    }
}
