using System;
using System.Reactive;


namespace Shiny.BluetoothLE
{
    public interface IGattReliableWriteTransaction : IDisposable
    {
        TransactionState Status { get; }
        IObservable<GattCharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value);
        IObservable<Unit> Commit();
        void Abort();
    }
}