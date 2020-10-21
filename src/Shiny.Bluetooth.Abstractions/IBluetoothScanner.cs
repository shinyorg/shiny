using System;


namespace Shiny.Bluetooth
{
    public interface IBluetoothScanner
    {
        // TODO: scanresult with rssi & service uuids - as well as filters
        IObservable<IBluetoothDevice> Scan();
    }
}
