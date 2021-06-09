using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using Shiny.BluetoothLE.Internals;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService;


namespace Shiny.BluetoothLE
{
    public class GattService : AbstractGattService
    {
        readonly PeripheralContext context;
        readonly Native native;


        public GattService(PeripheralContext context, Native native) : base(context.Peripheral, native.Uuid.ToString(), false)
        {
            this.context = context;
            this.native = native;
        }


        public override IObservable<IGattCharacteristic?> GetKnownCharacteristic(string characteristicUuid, bool throwIfNotFound = false) =>
            Observable.FromAsync(async () =>
            {
                var uuid = Utils.ToUuidType(characteristicUuid);
                var result = await this.native.GetCharacteristicsForUuidAsync(
                    uuid,
                    BluetoothCacheMode.Cached
                );
                if (result.Status != GattCommunicationStatus.Success)
                    throw new ArgumentException("GATT Communication failure - " + result.Status);

                var ch = new GattCharacteristic(this.context, result.Characteristics.First(), this);
                return ch;
            })
            .Assert(this.Uuid, characteristicUuid, throwIfNotFound);


        public override IObservable<IList<IGattCharacteristic>> GetCharacteristics() => Observable.FromAsync(async ct =>
        {
            var result = await this.native
                .GetCharacteristicsAsync(BluetoothCacheMode.Uncached)
                .AsTask(ct)
                .ConfigureAwait(false);

            result.Status.Assert();
            return result
                .Characteristics
                .Select(x => new GattCharacteristic(this.context, x, this))
                .Cast<IGattCharacteristic>()
                .ToList();
        });
    }
}
