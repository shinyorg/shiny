using System;
using ExternalAccessory;


namespace Shiny.Bluetooth
{
    public class ShinyBluetoothDevice : IBluetoothDevice
    {
        public ShinyBluetoothDevice(EAAccessory accessory)
        {

        }

        public string Name => throw new NotImplementedException();

        public IObservable<object> Connect()
        {
            throw new NotImplementedException();
        }

        public IObservable<object> Disconnect()
        {
            throw new NotImplementedException();
        }

        public IObservable<byte[]> Read()
        {
            throw new NotImplementedException();
        }

        public IObservable<object> Write(byte[] data)
        {
            throw new NotImplementedException();
        }
    }
}
