using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor;


namespace Shiny.BluetoothLE
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        readonly Native native;


        public GattDescriptor(Native native, IGattCharacteristic characteristic) : base(characteristic, native.Uuid.ToString())
            => this.native = native;


        public override IObservable<GattDescriptorResult> Write(byte[] data) => Observable.FromAsync(async ct =>
        {
            await this.native.WriteValueAsync(data.AsBuffer()).Execute(ct);
            return new GattDescriptorResult(this, data);
        });


        public override IObservable<GattDescriptorResult> Read() => Observable.FromAsync(async ct =>
        {
            var result = await this.native
                .ReadValueAsync(BluetoothCacheMode.Uncached)
                .AsTask(ct)
                .ConfigureAwait(false);
            result.Status.Assert();

            var value = result.Value?.ToArray();
            return new GattDescriptorResult(this, value);
        });
    }
}
