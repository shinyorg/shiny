using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public abstract class AbstractGattService : IGattService
    {
        protected AbstractGattService(IPeripheral peripheral, string uuid, bool primary)
        {
            this.Peripheral = peripheral;
            this.Uuid = uuid;
            this.IsPrimary = primary;
        }


        public IPeripheral Peripheral { get; }
        public string Uuid { get; }
        public bool IsPrimary { get; }

        public abstract IObservable<IList<IGattCharacteristic>> GetCharacteristics();
        public virtual IObservable<IGattCharacteristic?> GetKnownCharacteristic(string characteristicUuid, bool throwIfNotFound = false) =>
            this
                .GetCharacteristics()
                .Select(x => x.FirstOrDefault(y => y.Uuid.Equals(characteristicUuid)))
                .Assert(this.Uuid, characteristicUuid, throwIfNotFound);
    }
}
