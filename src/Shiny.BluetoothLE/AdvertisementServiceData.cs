namespace Shiny.BluetoothLE;

public record AdvertisementServiceData(
    string Uuid,
    byte[] Data
);