namespace Shiny.BluetoothLE;


public class AdvertisementData : IAdvertisementData
{
    readonly JsScanResult sr;
    public AdvertisementData(JsScanResult sr) => this.sr = sr;

    public string? LocalName => this.sr.DeviceName;
    public bool? IsConnectable => null;
    public AdvertisementServiceData[]? ServiceData => null;
    public ManufacturerData? ManufacturerData => null;
    public string[]? ServiceUuids => this.sr.ServiceUuids;
    public int? TxPower => this.sr.TxPower == 0 ? null : this.sr.TxPower;
}
