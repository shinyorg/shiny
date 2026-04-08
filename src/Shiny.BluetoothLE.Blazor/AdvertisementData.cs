namespace Shiny.BluetoothLE;


public class AdvertisementData(JsScanResult sr) : IAdvertisementData
{
    public string? LocalName => sr.DeviceName;
    public bool? IsConnectable => null;
    public AdvertisementServiceData[]? ServiceData => null;
    public ManufacturerData? ManufacturerData => null;
    public string[]? ServiceUuids => sr.ServiceUuids;
    public int? TxPower => sr.TxPower == 0 ? null : sr.TxPower;
}
