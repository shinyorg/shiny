using System;

namespace Shiny.BluetoothLE;


public class AdvertisementData : IAdvertisementData
{
    readonly JsScanResult sr;
    public AdvertisementData(JsScanResult sr) => this.sr = sr;

    public string? LocalName => "TODO";
    public bool? IsConnectable => null;
    public AdvertisementServiceData[]? ServiceData => null;
    public ManufacturerData? ManufacturerData => null;
    public string[]? ServiceUuids => null;
    public int? TxPower => null;
}

