using System;
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

        public abstract IObservable<IGattCharacteristic> DiscoverCharacteristics();
        public virtual IObservable<IGattCharacteristic> GetKnownCharacteristics(params string[] characteristicIds)
            => this.DiscoverCharacteristics().Where(x => characteristicIds.Any(y => y == x.Uuid));
    }
}
