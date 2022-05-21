namespace Shiny.BluetoothLE;


public record ScanResult(
    IPeripheral Peripheral,
    int Rssi,
    IAdvertisementData AdvertisementData
);
