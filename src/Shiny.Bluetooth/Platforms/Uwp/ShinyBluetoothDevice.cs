using System;
using Windows.Devices.Bluetooth.Rfcomm;


namespace Shiny.Bluetooth
{
    public class ShinyBluetoothDevice : IBluetoothDevice
    {
        readonly RfcommDeviceService device;


        public ShinyBluetoothDevice(RfcommDeviceService device)
        {
            this.device = device;
        }

        public string Name => this.device.Device.Name;

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
