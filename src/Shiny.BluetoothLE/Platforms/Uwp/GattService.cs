using System;
using System.Linq;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattDeviceService;
using Shiny.BluetoothLE.Internals;
using System.Collections.Generic;

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


        public override IObservable<IGattCharacteristic?> GetKnownCharacteristic(string characteristicUuid, bool throwIfNotFound = false) =>
            Observable.FromAsync(async () =>
            {
                var result = await this.native.GetCharacteristicsForUuidAsync(
                    Guid.Parse(characteristicUuid),
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
