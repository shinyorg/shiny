namespace Shiny.BluetoothLE;

public record AdvertisementServiceData(
    string Uuid,
    byte[] Data
);

public record ManufacturerData(
    ushort CompanyId,
    byte[] Data
);


public interface IAdvertisementData
{
    string? LocalName { get; }
    bool? IsConnectable { get; }
    AdvertisementServiceData[]? ServiceData { get; }
    ManufacturerData? ManufacturerData { get; }
    string[]? ServiceUuids { get; }
    int? TxPower { get; }
}
