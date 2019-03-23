using System;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;


namespace Shiny.BluetoothLE.Central
{
    public partial class GattCharacteristic : AbstractGattCharacteristic
    {

        public override IObservable<CharacteristicGattResult> WriteWithoutResponse(byte[] value)
        {
            var data = NSData.FromArray(value);
            this.Peripheral.WriteValue(data, this.NativeCharacteristic, CBCharacteristicWriteType.WithoutResponse);
            return Observable.Return(new CharacteristicGattResult(this, value));
        }

        //public override IObservable<CharacteristicGattResult> WriteWithoutResponse(byte[] value)
        //{
        //    if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
        //        return this.NewInternalWrite(value);

        //    return Observable.Return(this.InternalWrite(value));
        //}


        //IObservable<CharacteristicGattResult> NewInternalWrite(byte[] value) => Observable.Create<CharacteristicGattResult>(ob =>
        //{
        //    EventHandler handler = null;
        //    if (this.Peripheral.CanSendWriteWithoutResponse)
        //    {
        //        ob.Respond(this.InternalWrite(value));
        //    }
        //    else
        //    {
        //        handler = new EventHandler((sender, args) => ob.Respond(this.InternalWrite(value)));
        //        this.Peripheral.IsReadyToSendWriteWithoutResponse += handler;
        //    }
        //    return () =>
        //    {
        //        if (handler != null)
        //            this.Peripheral.IsReadyToSendWriteWithoutResponse -= handler;
        //    };
        //});


        //CharacteristicGattResult InternalWrite(byte[] value)
        //{
        //    var data = NSData.FromArray(value);
        //    this.Peripheral.WriteValue(data, this.NativeCharacteristic, CBCharacteristicWriteType.WithoutResponse);
        //    return new CharacteristicGattResult(this, value);
        //}
    }
}