namespace Shiny.BluetoothLE;


public record ScanConfig(
    /// <summary>
    /// Filters scan to peripherals that advertise specified service UUIDs
    /// iOS - you must set this to initiate a background scan
    /// </summary>
    params string[] ServiceUuids
);


public record ScanResult(
    IPeripheral Peripheral,
    int Rssi,
    IAdvertisementData AdvertisementData
);


public record BleServiceInfo(string Uuid);


public record BleCharacteristicInfo(
    BleServiceInfo Service,
    string Uuid,
    bool IsNotifying,
    CharacteristicProperties Properties
);


public record BleDescriptorInfo(
    BleCharacteristicInfo Characteristic,
    string Uuid
);


public record BleCharacteristicResult(
    BleCharacteristicInfo Characteristic,
    BleCharacteristicEvent Event,
    byte[]? Data
);


public enum BleCharacteristicEvent
{
    Read,
    Write,
    WriteWithoutResponse,
    Notification
}


public record BleDescriptorResult(
    BleDescriptorInfo Descriptor,
    byte[]? Data
);