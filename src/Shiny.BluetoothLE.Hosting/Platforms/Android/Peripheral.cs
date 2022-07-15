using System;
using System.Linq;
using Android.Bluetooth;

namespace Shiny.BluetoothLE.Hosting;


public class Peripheral : IPeripheral
{
    readonly Lazy<string> deviceUuidLazy;


    public Peripheral(BluetoothDevice native)
    {
        this.Native = native;
        this.deviceUuidLazy = new Lazy<string>(() =>
        {
            var deviceGuid = new byte[16];
            var mac = native.Address.Replace(":", "");
            var macBytes = Enumerable
                .Range(0, mac.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(mac.Substring(x, 2), 16))
                .ToArray();

            macBytes.CopyTo(deviceGuid, 10);
            //return new Guid(deviceGuid);
            return macBytes.ToString();
        });
    }


    public BluetoothDevice Native { get; }
    public string Uuid => this.deviceUuidLazy.Value;
    public object Context { get; set; }
    public int Mtu { get; internal set; } = 20; // Default MTU size from BLE spec
}
