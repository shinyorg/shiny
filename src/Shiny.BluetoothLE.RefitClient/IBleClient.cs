using System;
using System.Reactive;


namespace Shiny.BluetoothLE.RefitClient
{
    public interface IBleClient
    {
        IPeripheral Peripheral { get; }
        IObservable<Unit> Connect();
        void Disconnect();
    }
}
