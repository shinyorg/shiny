using System;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;


namespace Shiny.BluetoothLE
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        readonly GattCharacteristic characteristicObj;
        readonly CBDescriptor native;

        public CBCharacteristic NativeCharacteristic => this.characteristicObj.NativeCharacteristic;
        public CBService NativeService => this.characteristicObj.NativeService;
        public CBPeripheral Peripheral => this.characteristicObj.Peripheral;


        public GattDescriptor(GattCharacteristic characteristic, CBDescriptor native)
                       : base(characteristic, native.UUID.ToString())
        {
            this.characteristicObj = characteristic;
            this.native = native;
        }


        public override IObservable<GattDescriptorResult> Read() => Observable.Create<GattDescriptorResult>(ob =>
        {
            var handler = new EventHandler<CBDescriptorEventArgs>((sender, args) =>
            {
                if (!this.Equals(args.Descriptor))
                    return;

                if (args.Error != null)
                {
                    ob.OnError(new BleException(args.Error.Description));
                }
                else
                {
                    var value = args.Descriptor.ToByteArray();
                    ob.Respond<GattDescriptorResult>(new GattDescriptorResult(this, value));
                }
            });
            this.Peripheral.UpdatedValue += handler;
            this.Peripheral.ReadValue(this.native);

            return () => this.Peripheral.UpdatedValue -= handler;
        });


        public override IObservable<GattDescriptorResult> Write(byte[] data) => Observable.Create<GattDescriptorResult>(ob =>
        {
            var handler = new EventHandler<CBDescriptorEventArgs>((sender, args) =>
            {
                if (!this.Equals(args.Descriptor))
                    return;

                if (args.Error != null)
                    ob.OnError(new BleException(args.Error.Description));
                else
                {
                    var bytes = args.Descriptor.ToByteArray();
                    ob.Respond(new GattDescriptorResult(this, bytes));
                }
            });

            var nsdata = NSData.FromArray(data);
            this.Peripheral.WroteDescriptorValue += handler;
            this.Peripheral.WriteValue(nsdata, this.native);

            return () => this.Peripheral.WroteDescriptorValue -= handler;
        });


        bool Equals(CBDescriptor descriptor)
        {
            if (!this.native.UUID.Equals(descriptor.UUID))
                return false;

            if (!this.NativeCharacteristic.UUID.Equals(descriptor.Characteristic.UUID))
                return false;

            if (!this.NativeService.UUID.Equals(descriptor.Characteristic.Service.UUID))
                return false;

			if (!this.Peripheral.Identifier.Equals(descriptor.Characteristic.Service.Peripheral.Identifier))
                return false;

            return true;
        }


        public override bool Equals(object obj)
        {
            var other = obj as GattDescriptor;
            if (other == null)
                return false;

			if (!Object.ReferenceEquals(this, other))
                return false;

            return true;
        }


        public override int GetHashCode() => this.native.GetHashCode();
        public override string ToString() => this.Uuid.ToString();
    }
}