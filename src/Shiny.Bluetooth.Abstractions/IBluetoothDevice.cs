using System;


namespace Shiny.Bluetooth
{
    public interface IBluetoothDevice
    {
        string Name { get; }
        IObservable<object> Connect();
        IObservable<object> Disconnect();
        IObservable<object> Write(byte[] data);
        IObservable<byte[]> Read();
    }
}
