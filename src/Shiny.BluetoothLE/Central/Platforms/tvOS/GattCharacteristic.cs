using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;


namespace Acr.BluetoothLE.Central
{
    public partial class GattCharacteristic : AbstractGattCharacteristic
    {
        public override IObservable<CharacteristicGattResult> WriteWithoutResponse(byte[] value) => Observable.Create<CharacteristicGattResult>(ob =>
        {
            this.AssertWrite(false);

            //var handler = new EventHandler((sender, args) => this.WriteWithoutResponse(ob, CBCharacteristicWriteType.WithoutResponse, value));
            //this.Peripheral.IsReadyToSendWriteWithoutResponse += handler;

            var type = this.Peripheral.CanSendWriteWithoutResponse
                ? CBCharacteristicWriteType.WithoutResponse
                : CBCharacteristicWriteType.WithResponse;
            this.Write(ob, type, value);

            //return () => this.Peripheral.IsReadyToSendWriteWithoutResponse -= handler;
            return Disposable.Empty;
        });


        void Write(IObserver<CharacteristicGattResult> ob, CBCharacteristicWriteType type, byte[] value)
        {
            var data = NSData.FromArray(value);
            this.Peripheral.WriteValue(data, this.NativeCharacteristic, type);
            ob.Respond(new CharacteristicGattResult(this, value));
        }
    }
}