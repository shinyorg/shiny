using System;
using System.Reactive;
using System.Reactive.Linq;
using Shiny.BluetoothLE.Internals;


namespace Shiny.BluetoothLE
{
    public class GattReliableWriteTransaction : AbstractGattReliableWriteTransaction
    {
        readonly PeripheralContext context;


        public GattReliableWriteTransaction(PeripheralContext context)
        {
            this.context = context;
            this.context.Gatt.BeginReliableWrite();
        }


        public override IObservable<GattCharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value)
        {
            // just write to the standard characteristic write
            this.AssertAction();
            return characteristic.Write(value);
        }


        public override IObservable<Unit> Commit() => this.context.Invoke(Observable.Create<Unit>(ob =>
        {
            this.AssertAction();

            var sub = this.context
                .Callbacks
                .ReliableWriteCompleted
                .Subscribe(args =>
                {
                    if (args.IsSuccessful)
                    {
                        this.Status = TransactionState.Committed;
                        ob.Respond(Unit.Default);
                    }
                    else
                    {
                        this.Status = TransactionState.Aborted;
                        ob.OnError(new GattReliableWriteTransactionException("Error committing transaction"));
                    }
                });

            if (!this.context.Gatt.ExecuteReliableWrite())
                throw new GattReliableWriteTransactionException("Failed to execute write");

            this.Status = TransactionState.Committing;

            return sub;
        }));


        public override void Abort()
        {
            this.AssertAction();
            this.context.Gatt.AbortReliableWrite();
            this.Status = TransactionState.Aborted;
        }


        protected override void Dispose(bool disposing)
        {
        }
    }
}