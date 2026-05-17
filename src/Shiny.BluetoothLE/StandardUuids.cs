namespace Shiny.BluetoothLE;

/// <summary>
/// Common Bluetooth SIG assigned UUIDs for well-known services and characteristics.
/// </summary>
public static class StandardUuids
{
    /// <summary>
    /// The Device Information Service UUID (0x180A).
    /// </summary>
    public const string DeviceInformationServiceUuid = "180A";

    /// <summary>
    /// The Heart Rate Service and Heart Rate Measurement characteristic UUIDs (0x180D / 0x2A37).
    /// </summary>
    public static (string ServiceUuid, string CharacteristicUuid) HeartRateMeasurementSensor => ("180D", "2A37");

    /// <summary>
    /// The Battery Service and Battery Level characteristic UUIDs (0x180F / 0x2A19).
    /// </summary>
    public static (string ServiceUuid, string CharacteristicUuid) BatteryService => ("180F", "2A19");
}
