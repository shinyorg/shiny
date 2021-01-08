using System;
using System.Linq;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService;
using Shiny.BluetoothLE.Internals;


namespace Shiny.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        readonly DeviceContext context;
        readonly Native native;


        public GattService(DeviceContext context, Native native) : base(context.Peripheral, native.Uuid.ToString(), false)
        {
            this.context = context;
            this.native = native;
        }


        public override IObservable<IGattCharacteristic> GetKnownCharacteristic(string characteristicUuid) => Observable.FromAsync(async () =>
        {
            var result = await this.native.GetCharacteristicsForUuidAsync(Guid.Parse(characteristicUuid), BluetoothCacheMode.Cached);
            if (result.Status != GattCommunicationStatus.Success)
                throw new ArgumentException("GATT Communication failure - " + result.Status);

            if (!result.Characteristics.Any())
                throw new ArgumentException("No characteristic found for " + characteristicUuid);

            var ch = new GattCharacteristic(this.context, result.Characteristics.First(), this);
            return ch;
        });


        public override IObservable<IGattCharacteristic> DiscoverCharacteristics() => Observable.Create<IGattCharacteristic>(async ob =>
        {
            var result = await this.native.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            foreach (var characteristic in result.Characteristics)
            {
                var wrap = new GattCharacteristic(this.context, characteristic, this);
                ob.OnNext(wrap);
            }

            ob.OnCompleted();
        });
    }
}
