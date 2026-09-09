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
