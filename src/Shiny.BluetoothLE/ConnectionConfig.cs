namespace Shiny.BluetoothLE;


/// <summary>
/// Options applied when connecting to a peripheral.
/// </summary>
/// <param name="AutoConnect">
/// On Android, when true the OS will automatically (re)connect once the peripheral comes back into range or after a disconnect;
/// setting false speeds up the initial connection but disables auto-reconnect. On iOS, controls whether reconnection is attempted automatically.
/// </param>
public record ConnectionConfig(
    bool AutoConnect = true
);
