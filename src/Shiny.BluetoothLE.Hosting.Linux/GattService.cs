using System;
using System.Collections.Generic;
using System.Linq;

namespace Shiny.BluetoothLE.Hosting;


public class GattService : IGattService, IGattServiceBuilder
{
    readonly List<GattCharacteristic> chars = new();


    public GattService(string uuid, bool primary)
    {
        this.Uuid = uuid;
        this.Primary = primary;
    }


    public string Uuid { get; }
    public bool Primary { get; }

    /// <summary>
    /// BlueZ object path for this service. Set when registered with the GATT manager.
    /// </summary>
    public string? ObjectPath { get; internal set; }

    public IReadOnlyList<IGattCharacteristic> Characteristics => this.chars.Cast<IGattCharacteristic>().ToList();
    internal IReadOnlyList<GattCharacteristic> NativeCharacteristics => this.chars;


    public IGattCharacteristic AddCharacteristic(string uuid, Action<IGattCharacteristicBuilder> characteristicBuilder)
    {
        var ch = new GattCharacteristic(uuid);
        characteristicBuilder(ch);
        this.chars.Add(ch);
        return ch;
    }
}
