using System;
using CoreBluetooth;

namespace Shiny.BluetoothLE.Hosting;


public class Peripheral : IPeripheral
{
    public Peripheral(CBCentral central)
    {
        this.Central = central;
        this.Uuid = central.Identifier.ToString();
    }


    public string Uuid { get; }
    public CBCentral Central { get; }
    public object? Context { get; set; }
    public int Mtu => (int)this.Central.MaximumUpdateValueLength;
}