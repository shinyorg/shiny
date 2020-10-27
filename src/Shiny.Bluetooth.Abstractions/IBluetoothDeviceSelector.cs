using System;


namespace Shiny.Bluetooth
{
    public interface IBluetoothDeviceSelector
    {
        // TODO: may need EAprotocol filter?  Other?
        IObservable<IBluetoothDevice> Select();
    }
}
