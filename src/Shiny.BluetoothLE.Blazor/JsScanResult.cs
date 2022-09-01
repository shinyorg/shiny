using System.Text.Json.Serialization;

namespace Shiny.BluetoothLE;

public class JsScanResult
{
    public string DeviceId { get; set; } = null!;
    public string? DeviceName { get; set; }

    [JsonPropertyName("rssi")]
    public int Rssi { get; set; }

    [JsonPropertyName("uuids")]
    public string[]? ServiceUuids { get; set; }

    [JsonPropertyName("txPower")]
    public int TxPower { get; set; }

    // manufacturer data?
}
