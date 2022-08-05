namespace Shiny.BluetoothLE;

public class JsScanResult
{
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }

    public int Rssi { get; set; }
    public string[] ServiceUuids { get; set; }
    public int TxPower { get; set; }

    // manufacturer data
    // service data
}
