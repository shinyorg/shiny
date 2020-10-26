using System;


namespace Shiny.Bluetooth
{
    public interface IBluetoothDeviceSelector
    {
        IObservable<IBluetoothDevice> Select();
    }
}
