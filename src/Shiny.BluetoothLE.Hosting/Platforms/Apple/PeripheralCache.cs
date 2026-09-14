using System;
using System.Collections.Generic;
using System.Linq;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE.Hosting;


// written from CoreBluetooth's queue, read from whichever thread calls SubscribedCentrals or Notify
class PeripheralCache
{
    readonly object syncLock = new();
    readonly Dictionary<NSUuid, Peripheral> subscribed = new();
    readonly Dictionary<NSUuid, Peripheral> peripherals = new();


    public IReadOnlyList<Peripheral> Subscribed
    {
        get
        {
            lock (this.syncLock)
                return this.subscribed.Values.ToList();
        }
    }


    public Peripheral GetOrAdd(CBCentral central)
    {
        lock (this.syncLock)
        {
            if (!this.peripherals.TryGetValue(central.Identifier, out var peripheral))
            {
                peripheral = new Peripheral(central);
                this.peripherals.Add(central.Identifier, peripheral);
            }
            return peripheral;
        }
    }


    public Peripheral SetSubscription(CBCentral central, bool subscribe)
    {
        lock (this.syncLock)
        {
            var peripheral = this.GetOrAdd(central);
            if (subscribe)
                this.subscribed[central.Identifier] = peripheral;
            else
                this.subscribed.Remove(central.Identifier);

            return peripheral;
        }
    }
}
