using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Mono.BlueZ.DBus;


namespace Plugin.BluetoothLE
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        readonly GattDescriptor1 native;


        public GattDescriptor(GattDescriptor1 native, IGattCharacteristic characteristic) : base(characteristic, Guid.Parse(native.UUID))
        {
            this.native = native;
        }


        public override IObservable<DescriptorResult> Write(byte[] data) => Observable.Create<DescriptorResult>(ob =>
        {
            this.native.WriteValue(data);
            ob.Respond(new DescriptorResult(this, DescriptorEvent.Write, data));
            return Disposable.Empty;
        });


        public override IObservable<DescriptorResult> Read() => Observable.Create<DescriptorResult>(ob =>
        {
            var data = this.native.ReadValue();
            ob.Respond(new DescriptorResult(this, DescriptorEvent.Read, data));
            return Disposable.Empty;
        });
    }
}
