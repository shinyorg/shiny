namespace Shiny.Tests.BluetoothLE;

public class BleConfiguration
{
    public TimeSpan DeviceScanTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public string PeripheralName { get; set; }
    public string ServiceUuid { get; set; }

    public string NotifyCharacteristicUuid { get; set; }
    public string ReadCharacteristicUuid { get; set; }
    public string WriteCharacteristicUuid { get; set; }
}
