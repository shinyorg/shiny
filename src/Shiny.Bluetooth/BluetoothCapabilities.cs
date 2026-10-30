namespace Shiny.Bluetooth;


/// <summary>
/// The operations <see cref="IBluetoothManager"/> can perform on the current platform.
/// </summary>
[Flags]
public enum BluetoothCapabilities
{
    None = 0,

    /// <summary><see cref="IBluetoothManager.Discover"/> - every platform.</summary>
    Discovery = 1,

    /// <summary><see cref="IBluetoothDevice.Pair"/> - every platform.</summary>
    Pairing = 2,

    /// <summary><see cref="IBluetoothDevice.Connect"/> - every platform.</summary>
    Connect = 4,

    /// <summary><see cref="IBluetoothManager.Listen"/> - every platform.</summary>
    Listen = 8,

    /// <summary><see cref="IBluetoothManager.RequestDiscoverable"/> - Android and Linux only.</summary>
    Discoverable = 16
}
