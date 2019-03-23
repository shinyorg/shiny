using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


namespace Shiny.BluetoothLE.Peripherals
{
    public class GattService : IGattService, IGattServiceBuilder, IDisposable
    {
        readonly List<GattCharacteristic> characteristics;
        GattServiceProviderResult native;


        public GattService(Guid uuid)
        {
            this.Uuid = uuid;
            this.characteristics = new List<GattCharacteristic>();
        }


        public Guid Uuid { get; }
        public bool Primary => false;


        public IReadOnlyList<IGattCharacteristic> Characteristics =>
            this.characteristics.Cast<IGattCharacteristic>().ToList();


        public IGattCharacteristic AddCharacteristic(Guid uuid, Action<IGattCharacteristicBuilder> characteristicBuilder)
        {
            var ch = new GattCharacteristic(uuid);
            characteristicBuilder(ch);
            this.characteristics.Add(ch);
            return ch;
        }


        public async Task Build()
        {
            this.native = await GattServiceProvider.CreateAsync(this.Uuid);
            foreach (var ch in this.characteristics)
                await ch.Build(this.native);

            this.native.ServiceProvider.StartAdvertising(new GattServiceProviderAdvertisingParameters
            {
                IsConnectable = true,
                IsDiscoverable = true
            });
        }


        public void Dispose()
        {
            this.native?.ServiceProvider.StopAdvertising();
            foreach (var ch in this.characteristics)
                ch.Dispose();
        }
    }
}