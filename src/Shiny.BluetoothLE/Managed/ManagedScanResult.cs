using System;

namespace Shiny.BluetoothLE.Managed;

public class ManagedScanResult : NotifyPropertyChanged, IAdvertisementData
{
    public ManagedScanResult(IPeripheral peripheral) => this.Peripheral = peripheral;


    public IPeripheral Peripheral { get; }
    public bool IsConnected => this.Peripheral.Status == ConnectionState.Connected;
    public string Uuid => this.Peripheral.Uuid;


    bool? connectable;
    public bool? IsConnectable
    {
        get => this.connectable;
        internal set => this.Set(ref this.connectable, value);
    }


    string name;
    public string Name
    {
        get => this.name;
        internal set => this.Set(ref this.name, value);
    }


    int rssi;
    public int Rssi
    {
        get => this.rssi;
        internal set => this.Set(ref this.rssi, value);
    }


    int? txPower;
    public int? TxPower
    {
        get => this.txPower;
        internal set => this.Set(ref this.txPower, value);
    }


    ManufacturerData? manufacturerData;
    public ManufacturerData? ManufacturerData
    {
        get => this.manufacturerData;
        internal set => this.Set(ref this.manufacturerData, value);
    }


    DateTimeOffset lastSeen;
    public DateTimeOffset LastSeen
    {
        get => this.lastSeen;
        internal set => this.Set(ref this.lastSeen, value);
    }


    string? localName;
    public string? LocalName
    {
        get => this.localName;
        internal set => this.Set(ref this.localName, value);
    }


    public string[]? ServiceUuids { get; internal set; }
    public AdvertisementServiceData[]? ServiceData { get; internal set; }
}
