using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Shiny.BluetoothLE.Hosting;


public class GattService : IGattService, IGattServiceBuilder, IDisposable
{
    readonly List<GattCharacteristic> characteristics;
    GattServiceProviderResult? native;


    public GattService(string uuid, bool primary)
    {
        this.Uuid = uuid;
        this.Primary = primary;
        this.characteristics = new List<GattCharacteristic>();
    }


    public string Uuid { get; }
    public bool Primary { get; }


    public IReadOnlyList<IGattCharacteristic> Characteristics =>
        this.characteristics.Cast<IGattCharacteristic>().ToList();


    public IGattCharacteristic AddCharacteristic(string uuid, Action<IGattCharacteristicBuilder> characteristicBuilder)
    {
        var ch = new GattCharacteristic(uuid);
        characteristicBuilder(ch);
        this.characteristics.Add(ch);
        return ch;
    }


    public async Task Build()
    {
        this.native = await GattServiceProvider.CreateAsync(UuidHelper.ToUuid(this.Uuid));
        foreach (var ch in this.characteristics)
            await ch.Build(this.native);
    }


    public void Dispose()
    {
        foreach (var ch in this.characteristics)
            ch.Dispose();
    }
}
