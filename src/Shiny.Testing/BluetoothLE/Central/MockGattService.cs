using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Shiny.BluetoothLE.Central;


namespace Shiny.Testing.BluetoothLE.Central
{
    public class MockGattService : IGattService
    {

        public IPeripheral Peripheral { get; set; }
        public Guid Uuid => this.Peripheral?.Uuid ?? Guid.NewGuid();
        public string Description { get; set; }


        public IList<IGattCharacteristic> Characteristics { get; set; } = new List<IGattCharacteristic>();
        public IObservable<IGattCharacteristic> DiscoverCharacteristics() => this.Characteristics.ToObservable();
        public IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds)
            => this.DiscoverCharacteristics().Where(x => characteristicIds.Any(y => y == x.Uuid));
    }
}
