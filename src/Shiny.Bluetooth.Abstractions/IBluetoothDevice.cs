using System;
using System.Reactive;

namespace Shiny.Bluetooth
{
    // TODO: pairing interface
    public interface IBluetoothDevice
    {
        string Name { get; }

        ConnectionState Status { get; }
        IObservable<ConnectionState> WhenStatusChanged();

        IObservable<Unit> WhenDataAvailable();
        IObservable<Unit> Connect();
        IObservable<Unit> Disconnect();

        IObservable<Unit> Write(byte[] buffer, int offset, int length);
        IObservable<uint> Read(byte[] buffer, uint length = 1024);
    }
}
