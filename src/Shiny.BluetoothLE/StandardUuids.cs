namespace Shiny.BluetoothLE;

public static class StandardUuids
{
    public const string DeviceInformationServiceUuid = "180A";
    public static (string ServiceUuid, string CharacteristicUuid) HeartRateMeasurementSensor => ("180D", "2A37");

    public static (string ServiceUuid, string CharacteristicUuid) BatteryService => ("180F", "2A19");
}
