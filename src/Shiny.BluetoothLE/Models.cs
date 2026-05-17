namespace Shiny.BluetoothLE;


/// <summary>
/// Filters and platform-specific options applied to a BLE scan.
/// </summary>
/// <param name="ServiceUuids">Service UUIDs to filter advertisements by. On iOS, at least one UUID must be supplied to scan in the background.</param>
public record ScanConfig(
    params string[] ServiceUuids
);


/// <summary>
/// A single BLE advertisement observed during a scan.
/// </summary>
/// <param name="Peripheral">The peripheral that sent the advertisement.</param>
/// <param name="Rssi">The received signal strength in dBm.</param>
/// <param name="AdvertisementData">The decoded advertisement payload.</param>
public record ScanResult(
    IPeripheral Peripheral,
    int Rssi,
    IAdvertisementData AdvertisementData
);


/// <summary>
/// Metadata describing a remote GATT service.
/// </summary>
/// <param name="Uuid">The service UUID.</param>
public record BleServiceInfo(string Uuid);


/// <summary>
/// Metadata describing a remote GATT characteristic.
/// </summary>
/// <param name="Service">The owning service.</param>
/// <param name="Uuid">The characteristic UUID.</param>
/// <param name="IsNotifying">True when this app is currently subscribed to notifications/indications.</param>
/// <param name="Properties">The properties (read, write, notify, etc.) declared by the characteristic.</param>
public record BleCharacteristicInfo(
    BleServiceInfo Service,
    string Uuid,
    bool IsNotifying,
    CharacteristicProperties Properties
);


/// <summary>
/// Metadata describing a remote GATT descriptor.
/// </summary>
/// <param name="Characteristic">The owning characteristic.</param>
/// <param name="Uuid">The descriptor UUID.</param>
public record BleDescriptorInfo(
    BleCharacteristicInfo Characteristic,
    string Uuid
);


/// <summary>
/// Outcome of a characteristic operation.
/// </summary>
/// <param name="Characteristic">The characteristic the operation targeted.</param>
/// <param name="Event">The kind of operation that produced this result.</param>
/// <param name="Data">The payload (for reads/notifications) or the bytes written.</param>
public record BleCharacteristicResult(
    BleCharacteristicInfo Characteristic,
    BleCharacteristicEvent Event,
    byte[]? Data
);


/// <summary>
/// Identifies the GATT operation that produced a <see cref="BleCharacteristicResult"/>.
/// </summary>
public enum BleCharacteristicEvent
{
    /// <summary>Value was read from the characteristic.</summary>
    Read,

    /// <summary>Value was written to the characteristic with response.</summary>
    Write,

    /// <summary>Value was written to the characteristic without response.</summary>
    WriteWithoutResponse,

    /// <summary>A notification or indication was received from the characteristic.</summary>
    Notification
}


/// <summary>
/// Outcome of a descriptor read or write operation.
/// </summary>
/// <param name="Descriptor">The descriptor the operation targeted.</param>
/// <param name="Data">The descriptor value (for reads) or the bytes written.</param>
public record BleDescriptorResult(
    BleDescriptorInfo Descriptor,
    byte[]? Data
);
