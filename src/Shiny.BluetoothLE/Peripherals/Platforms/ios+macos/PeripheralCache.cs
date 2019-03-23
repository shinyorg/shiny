using System;
using System.Collections.Generic;
using System.Linq;
using CoreBluetooth;

namespace Shiny.BluetoothLE.Peripherals
{
    class PeripheralCache
    {
        readonly Dictionary<CBUUID, Peripheral> subscribed;
        readonly Dictionary<CBUUID, Peripheral> peripherals;


        public PeripheralCache()
        {
            this.subscribed = new Dictionary<CBUUID, Peripheral>();
            this.peripherals = new Dictionary<CBUUID, Peripheral>();
        }


        public IReadOnlyList<Peripheral> Subscribed => this.subscribed.Values.ToList();


        public Peripheral GetOrAdd(CBCentral central)
        {
            if (!this.peripherals.ContainsKey(central.UUID))
                this.peripherals.Add(central.UUID, new Peripheral(central));

            return this.peripherals[central.UUID];
        }


        public Peripheral SetSubscription(CBCentral central, bool subscribe)
        {
            var peripheral = this.GetOrAdd(central);
            if (subscribe)
            {
                this.subscribed.Add(central.UUID, peripheral);
            }
            else
            {
                this.subscribed.Remove(central.UUID);
            }
            return peripheral;
        }
    }
}
