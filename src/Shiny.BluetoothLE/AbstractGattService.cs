using System;
using System.Linq;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public abstract class AbstractGattService : IGattService
    {
        protected AbstractGattService(IPeripheral peripheral, Guid uuid, bool primary)
        {
            this.Peripheral = peripheral;
            this.Uuid = uuid;
            this.IsPrimary = primary;
        }


        public IPeripheral Peripheral { get; }
        public Guid Uuid { get; }
        public bool IsPrimary { get; }

        public abstract IObservable<IGattCharacteristic> DiscoverCharacteristics();

        public virtual string Description => Dictionaries.GetServiceDescription(this.Uuid);
        public virtual IObservable<IGattCharacteristic> GetKnownCharacteristics(params Guid[] characteristicIds)
            => this.DiscoverCharacteristics().Where(x => characteristicIds.Any(y => y == x.Uuid));
    }
}
