using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattDescriptor;


namespace Shiny.BluetoothLE
{
    public class GattDescriptor : AbstractGattDescriptor
    {
        readonly Native native;


        public GattDescriptor(Native native, IGattCharacteristic characteristic) : base(characteristic, native.Uuid)
        {
            this.native = native;
        }


        public override IObservable<DescriptorGattResult> Write(byte[] data) => Observable.FromAsync(async _ =>
        {
            var status = await this.native.WriteValueAsync(data.AsBuffer());
            if (status != GattCommunicationStatus.Success)
                throw new BleException($"Failed to write descriptor - {status}");

            return new DescriptorGattResult(this, data);
        });


        public override IObservable<DescriptorGattResult> Read() => Observable.FromAsync(async _ =>
        {
            var result = await this.native.ReadValueAsync(BluetoothCacheMode.Uncached);
            if (result.Status != GattCommunicationStatus.Success)
                throw new BleException($"Failed to read descriptor - {result.Status}");

            var value = result.Value?.ToArray();
            return new DescriptorGattResult(this, value);
        });
    }
}
