namespace Shiny.BluetoothLE;


public record ConnectionConfig(
    /// <summary>
    /// Android: Setting this to false will disable auto (re)connect when the peripheral
    /// is in range or when you disconnect.  However, it will speed up initial
    /// connections signficantly (defaults to true)
    /// iOS: Controls whether or not to reconnect automatically
    /// </summary>
    bool AutoConnect = true
);
