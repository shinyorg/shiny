using System;

namespace Shiny.BluetoothLE.Managed;

/// <summary>
/// An observable scan-result entry tracked by <see cref="IManagedScan"/>. Mutable advertisement
/// fields raise property-changed notifications when refreshed by subsequent advertisements.
/// </summary>
public class ManagedScanResult : NotifyPropertyChanged, IAdvertisementData
{
    /// <summary>
    /// Creates a new entry for the supplied peripheral.
    /// </summary>
    /// <param name="peripheral">The peripheral this entry tracks.</param>
    public ManagedScanResult(IPeripheral peripheral) => this.Peripheral = peripheral;

    /// <summary>
    /// Gets the most recent raw advertisement payload received from the peripheral.
    /// </summary>
    public IAdvertisementData? FullAdvertisementData { get; internal set; }

    /// <summary>
    /// Gets the peripheral this entry tracks.
    /// </summary>
    public IPeripheral Peripheral { get; }

    /// <summary>
    /// Gets a value indicating whether the underlying peripheral is currently connected.
    /// </summary>
    public bool IsConnected => this.Peripheral.Status == ConnectionState.Connected;

    /// <summary>
    /// Gets the peripheral identifier.
    /// </summary>
    public string Uuid => this.Peripheral.Uuid;


    bool? connectable;
    /// <summary>
    /// Gets whether the peripheral declared itself as connectable in its latest advertisement.
    /// </summary>
    public bool? IsConnectable
    {
        get => this.connectable;
        internal set => this.Set(ref this.connectable, value);
    }


    string? name;
    /// <summary>
    /// Gets the peripheral device name from its most recent advertisement, if any.
    /// </summary>
    public string? Name
    {
        get => this.name;
        internal set => this.Set(ref this.name, value);
    }


    int rssi;
    /// <summary>
    /// Gets the most recent received signal strength in dBm.
    /// </summary>
    public int Rssi
    {
        get => this.rssi;
        internal set => this.Set(ref this.rssi, value);
    }


    int? txPower;
    /// <summary>
    /// Gets the advertised transmit power level in dBm, if provided.
    /// </summary>
    public int? TxPower
    {
        get => this.txPower;
        internal set => this.Set(ref this.txPower, value);
    }


    ManufacturerData? manufacturerData;
    /// <summary>
    /// Gets the most recent manufacturer-specific advertisement data, if any.
    /// </summary>
    public ManufacturerData? ManufacturerData
    {
        get => this.manufacturerData;
        internal set => this.Set(ref this.manufacturerData, value);
    }


    DateTimeOffset lastSeen;
    /// <summary>
    /// Gets the UTC timestamp of the most recent advertisement received from this peripheral.
    /// </summary>
    public DateTimeOffset LastSeen
    {
        get => this.lastSeen;
        internal set => this.Set(ref this.lastSeen, value);
    }


    string? localName;
    /// <summary>
    /// Gets the local name advertised by the peripheral, if any.
    /// </summary>
    public string? LocalName
    {
        get => this.localName;
        internal set => this.Set(ref this.localName, value);
    }


    /// <summary>
    /// Gets the service UUIDs declared in the peripheral's latest advertisement.
    /// </summary>
    public string[]? ServiceUuids { get; internal set; }

    /// <summary>
    /// Gets the service-specific data entries in the peripheral's latest advertisement.
    /// </summary>
    public AdvertisementServiceData[]? ServiceData { get; internal set; }
}
