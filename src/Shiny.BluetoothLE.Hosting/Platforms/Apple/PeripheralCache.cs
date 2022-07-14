using System;
using System.Collections.Generic;
using System.Linq;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE.Hosting;


class PeripheralCache
{
    readonly Dictionary<NSUuid, Peripheral> subscribed;
    readonly Dictionary<NSUuid, Peripheral> peripherals;


    public PeripheralCache()
    {
        this.subscribed = new Dictionary<NSUuid, Peripheral>();
        this.peripherals = new Dictionary<NSUuid, Peripheral>();
    }


    public IReadOnlyList<Peripheral> Subscribed => this.subscribed.Values.ToList();


    public Peripheral GetOrAdd(CBCentral central)
    {
        if (!this.peripherals.ContainsKey(central.Identifier))
            this.peripherals.Add(central.Identifier, new Peripheral(central));

        return this.peripherals[central.Identifier];
    }


    public Peripheral SetSubscription(CBCentral central, bool subscribe)
    {
        var peripheral = this.GetOrAdd(central);
        if (subscribe)
        {
            this.subscribed.Add(central.Identifier, peripheral);
        }
        else
        {
            this.subscribed.Remove(central.Identifier);
        }
        return peripheral;
    }
}
