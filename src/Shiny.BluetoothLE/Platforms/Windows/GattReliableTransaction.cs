/*
 using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattReliableWriteTransaction;


namespace Shiny.BluetoothLE
{
    public class GattReliableWriteTransaction : AbstractGattReliableWriteTransaction
    {
        readonly Native native = new Native();


        public override IObservable<GattCharacteristicResult> Write(IGattCharacteristic characteristic, byte[] value)
        {
            this.AssertAction();

            if (!(characteristic is GattCharacteristic platform))
                throw new ArgumentException("Characteristic must be UWP type");

            this.native.WriteValue(platform.Native, value.AsBuffer());
            return Observable.Return(new GattCharacteristicResult(characteristic, value, GattCharacteristicResultType.Write));
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

            this.Status = TransactionState.Aborted;
        }
    }
}
 */