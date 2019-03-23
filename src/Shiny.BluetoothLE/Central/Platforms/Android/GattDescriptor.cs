using System;
using System.Reactive.Linq;
using Shiny.BluetoothLE.Central.Internals;
using Android.Bluetooth;


namespace Shiny.BluetoothLE.Central
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        readonly BluetoothGattDescriptor native;
        readonly DeviceContext context;


        public GattDescriptor(IGattCharacteristic characteristic,
                              DeviceContext context,
                              BluetoothGattDescriptor native) : base(characteristic, native.Uuid.ToGuid())
        {
            this.context = context;
            this.native = native;
        }


        public override byte[] Value => this.native.GetValue();


        public override IObservable<DescriptorGattResult> Write(byte[] data) =>
            this.context.Invoke(this.WriteInternal(data));


        protected internal IObservable<DescriptorGattResult> WriteInternal(byte[] data) => Observable.Create<DescriptorGattResult>(ob => //=> this.context.Invoke(Observable.Create<DescriptorGattResult>(ob =>
        {
            var sub = this.context
                .Callbacks
                .DescriptorWrite
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    if (args.IsSuccessful)
                        ob.Respond(new DescriptorGattResult(this, data));
                    else
                        ob.OnError(new BleException($"Failed to write descriptor value - {args.Status}"));
                });

            this.context.InvokeOnMainThread(() =>
            {
                try
                {
                    if (!this.native.SetValue(data))
                        ob.OnError(new BleException("Failed to set descriptor value"));

                    else if (!this.context.Gatt?.WriteDescriptor(this.native) ?? false)
                        ob.OnError(new BleException("Failed to write to descriptor"));
                }
                catch (Exception ex)
                {
                    ob.OnError(ex);
                }
            });

            return sub;
        });


        public override IObservable<DescriptorGattResult> Read() => this.context.Invoke(Observable.Create<DescriptorGattResult>(ob =>
        {
            var sub = this.context
                .Callbacks
                .DescriptorRead
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    if (args.IsSuccessful)
                        ob.Respond(new DescriptorGattResult(this, args.Descriptor.GetValue()));
                    else
                        ob.OnError(new BleException($"Failed to read descriptor value - {args.Status}"));
                });

            this.context.InvokeOnMainThread(() =>
            {
                try
                {
                    if (!this.context.Gatt?.ReadDescriptor(this.native) ?? false)
                        ob.OnError(new BleException("Failed to read descriptor"));
                }
                catch (Exception ex)
                {
                    ob.OnError(ex);
                }
            });

            return sub;
        }));


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
        public override string ToString() => $"Descriptor: {this.Uuid}";


        bool NativeEquals(GattDescriptorEventArgs args)
        {
            if (this.context?.Gatt == null)
                return false;

            if (args.Descriptor?.Characteristic?.Service == null)
                return false;

            if (args.Gatt == null)
                return false;

            if (this.native.Equals(args.Descriptor))
                return true;

            if (!this.native.Uuid.Equals(args.Descriptor.Uuid))
                return false;

            if (!this.native.Characteristic.Uuid.Equals(args.Descriptor.Characteristic.Uuid))
                return false;

            if (!this.native.Characteristic.Service.Uuid.Equals(args.Descriptor.Characteristic.Service.Uuid))
                return false;

            if (!this.context.Gatt.Equals(args.Gatt))
                return false;

            return true;
        }
    }
}