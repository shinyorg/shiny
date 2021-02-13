using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Shiny.BluetoothLE;


namespace Shiny.Testing.BluetoothLE
{
    public class TestGattService : IGattService
    {
        public TestGattService(IPeripheral peripheral, string uuid)
        {
            this.Peripheral = peripheral;
            this.Uuid = uuid;
        }


        public IPeripheral Peripheral { get; }
        public string Uuid { get; }


        public List<IGattCharacteristic> Characteristics { get; set; } = new List<IGattCharacteristic>();
        public IObservable<IList<IGattCharacteristic>> GetCharacteristics() =>
            this.Characteristics
                .Cast<IList<IGattCharacteristic>>()
                .ToObservable();

        public IObservable<IGattCharacteristic?> GetKnownCharacteristic(string characteristicUuid, bool throwIfNotFound = false) =>
            Observable.Return(
                this.Characteristics
                    .FirstOrDefault(
                        x => x.Uuid.Equals(characteristicUuid)
                    )
            )
            .Do(ch =>
            {
                if (throwIfNotFound && ch == null)
                    throw new ArgumentException($"Characteristic not found - {this.Uuid}/{characteristicUuid}");
            });
    }
}
