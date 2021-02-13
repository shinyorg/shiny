using System;
using System.Reactive;


namespace Shiny.BluetoothLE
{
    public abstract class AbstractGattReliableWriteTransaction : IGattReliableWriteTransaction
    {
        ~AbstractGattReliableWriteTransaction()
        {
            this.Dispose(false);
        }


        protected virtual void AssertAction()
        {
            if (this.Status != TransactionState.Active)
                throw new ArgumentException("Cannot perform action as transaction status is already " + this.Status);
        }


        public TransactionState Status { get; protected set; } = TransactionState.Active;
        public abstract IObservable<GattCharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value);
        public abstract IObservable<Unit> Commit();
        public abstract void Abort();


        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
