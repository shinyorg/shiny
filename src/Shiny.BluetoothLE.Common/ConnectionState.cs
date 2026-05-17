namespace Shiny.BluetoothLE;

/// <summary>
/// The GATT connection state between the central and a peripheral.
/// </summary>
public enum ConnectionState
{
    /// <summary>The peripheral is not connected.</summary>
    Disconnected,

    /// <summary>The peripheral is in the process of disconnecting.</summary>
    Disconnecting,

    /// <summary>The peripheral is connected and GATT operations are available.</summary>
    Connected,

    /// <summary>The peripheral is in the process of connecting.</summary>
    Connecting
}
