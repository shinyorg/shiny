using Shiny.BluetoothLE;

namespace Shiny.Printers.BluetoothLE;


/// <summary>A printer surfaced by a scan: the BLE peripheral plus the matched <see cref="BlePrinterConfig"/> to connect with.</summary>
public sealed record DiscoveredPrinter(
    IPeripheral Peripheral,
    string? Name,
    int Rssi,
    BlePrinterConfig Config
)
{
    /// <summary>The peripheral's stable UUID.</summary>
    public string Uuid => this.Peripheral.Uuid;
}
