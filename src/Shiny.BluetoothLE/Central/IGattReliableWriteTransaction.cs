using System;
using System.Reactive;


namespace Shiny.BluetoothLE.Central
{
    public interface IGattReliableWriteTransaction : IDisposable
    {
        TransactionState Status { get; }
        IObservable<CharacteristicGattResult> Write(IGattCharacteristic characteristic, byte[] value);
        IObservable<Unit> Commit();
        void Abort();
    }
}