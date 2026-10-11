using System.Text.Json.Serialization;

namespace Shiny.BluetoothLE;


/// <summary>
/// One manufacturer data entry from a Web Bluetooth advertisement.
/// </summary>
public class JsManufacturerData
{
    /// <summary>The Bluetooth SIG assigned company identifier.</summary>
    [JsonPropertyName("companyId")]
    public int CompanyId { get; set; }

    /// <summary>The payload, base64 encoded.</summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}


/// <summary>
/// One service data entry from a Web Bluetooth advertisement.
/// </summary>
public class JsAdvertisementServiceData
{
    /// <summary>The service UUID, in the 128-bit form Web Bluetooth reports.</summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    /// <summary>The payload, base64 encoded.</summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}


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

    /// <summary>
    /// Manufacturer data sections. Web Bluetooth reports these as a Map, flattened here.
    /// </summary>
    [JsonPropertyName("manufacturerData")]
    public JsManufacturerData[]? ManufacturerData { get; set; }

    /// <summary>
    /// Service data sections. Web Bluetooth reports these as a Map, flattened here.
    /// </summary>
    [JsonPropertyName("serviceData")]
    public JsAdvertisementServiceData[]? ServiceData { get; set; }
}
