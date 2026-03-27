namespace Shiny.BluetoothLE;

/// <summary>
/// Represents service data included in a BLE advertisement
/// </summary>
/// <param name="Uuid">The service UUID</param>
/// <param name="Data">The service data bytes</param>
public record AdvertisementServiceData(
    string Uuid,
    byte[] Data
);

/// <summary>
/// Represents manufacturer-specific data included in a BLE advertisement
/// </summary>
/// <param name="CompanyId">The Bluetooth SIG assigned company identifier</param>
/// <param name="Data">The manufacturer data bytes</param>
public record ManufacturerData(
    ushort CompanyId,
    byte[] Data
);


/// <summary>
/// Represents the data contained in a BLE advertisement packet
/// </summary>
public interface IAdvertisementData
{
    /// <summary>
    /// Gets the local name broadcast by the peripheral
    /// </summary>
    string? LocalName { get; }

    /// <summary>
    /// Gets whether the peripheral is connectable
    /// </summary>
    bool? IsConnectable { get; }

    /// <summary>
    /// Gets the service data entries in the advertisement
    /// </summary>
    AdvertisementServiceData[]? ServiceData { get; }

    /// <summary>
    /// Gets the manufacturer-specific data
    /// </summary>
    ManufacturerData? ManufacturerData { get; }

    /// <summary>
    /// Gets the advertised service UUIDs
    /// </summary>
    string[]? ServiceUuids { get; }

    /// <summary>
    /// Gets the transmit power level in dBm
    /// </summary>
    int? TxPower { get; }
}
