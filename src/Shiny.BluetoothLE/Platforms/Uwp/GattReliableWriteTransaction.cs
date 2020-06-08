using System;
using System.Reactive;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattReliableWriteTransaction;


namespace Shiny.BluetoothLE
{
    public class GattReliableWriteTransaction : AbstractGattReliableWriteTransaction
    {
        readonly Native native;


        public GattReliableWriteTransaction()
        {
            this.native = new Native();
        }


        public override IObservable<CharacteristicGattResult> Write(IGattCharacteristic characteristic, byte[] value)
        {
            this.AssertAction();

            if (!(characteristic is GattCharacteristic platform))
                throw new ArgumentException("Characteristic must be UWP type");

            // TODO: need write observable
            this.native.WriteValue(platform.Native, null);
            return null;
        }


        public override IObservable<Unit> Commit()
        {
            this.AssertAction();

            return Observable.FromAsync(async ct =>
            {
                this.Status = TransactionState.Committing;

                var result = await this.native.CommitAsync();
                if (result == GattCommunicationStatus.Success)
                {
                    this.Status = TransactionState.Committed;
                }
                else
                {
                    this.Status = TransactionState.Aborted;
                    throw new GattReliableWriteTransactionException("Failed to write transaction");
                }
            });
        }


        public override void Abort()
        {
            this.AssertAction();
            // TODO: how to abort?
        }
    }
}
