using System.Collections.Generic;
using System.Linq;
using Android.Bluetooth;

namespace Shiny.BluetoothLE;


public partial class BleManager 
{
    public IReadOnlyList<IPeripheral> GetPairedPeripherals() => this
        .Native!
        .Adapter!
        .BondedDevices!
        .Where(x => x.Type == BluetoothDeviceType.Dual || x.Type == BluetoothDeviceType.Le)
        .Select(this.GetPeripheral)
        .ToList();
}