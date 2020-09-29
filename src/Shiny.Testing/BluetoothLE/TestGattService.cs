using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Shiny.BluetoothLE;


namespace Shiny.Testing.BluetoothLE
{
    public class TestGattService : IGattService
    {
        public TestGattService(IPeripheral peripheral, Guid uuid)
        {
            this.Peripheral = peripheral;
            this.Uuid = uuid;
        }


        public IPeripheral Peripheral { get; }
        public Guid Uuid { get; }


        public List<IGattCharacteristic> Characteristics { get; set; } = new List<IGattCharacteristic>();
        public IObservable<IGattCharacteristic> DiscoverCharacteristics() => this.Characteristics.ToObservable();
        public IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds)
            => this.Characteristics.Where(x => characteristicIds.Any(y => y == x.Uuid)).ToObservable();
    }
}
