using System;
using System.Collections.Generic;
using System.Linq;
using CoreBluetooth;

namespace Shiny.BluetoothLE.Hosting;


public class GattService : IGattService, IGattServiceBuilder, IDisposable
{
    readonly CBPeripheralManager manager;
    readonly IList<GattCharacteristic> characteristics;


    public GattService(CBPeripheralManager manager, string uuid, bool primary)
    {
        this.manager = manager;

        this.Native = new CBMutableService(CBUUID.FromString(uuid), primary);
        this.characteristics = new List<GattCharacteristic>();
        this.Uuid = uuid;
        this.Primary = primary;
    }


    public CBMutableService Native { get; }
    public string Uuid { get; }
    public bool Primary { get; }
    public IReadOnlyList<IGattCharacteristic> Characteristics => this.characteristics.Cast<IGattCharacteristic>().ToList();


    public IGattCharacteristic AddCharacteristic(string uuid, Action<IGattCharacteristicBuilder> characteristicBuilder)
    {
        var ch = new GattCharacteristic(this.manager, uuid);
        characteristicBuilder(ch);
        ch.Build(this.Native);

        this.characteristics.Add(ch);
        return ch;
    }


    public void Dispose()
    {
        foreach (var ch in this.characteristics)
            ch.Dispose();
    }
}
