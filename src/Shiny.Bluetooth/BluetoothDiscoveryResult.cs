namespace Shiny.Bluetooth;


/// <summary>
/// One device heard by <see cref="IBluetoothManager.Discover"/>.
/// </summary>
/// <param name="Device">The device.</param>
/// <param name="Rssi">Signal strength in dBm from the inquiry response, or null when the platform did not report one.</param>
public record BluetoothDiscoveryResult(IBluetoothDevice Device, int? Rssi);
