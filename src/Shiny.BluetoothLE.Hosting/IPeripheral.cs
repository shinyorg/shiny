namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Represents a remote central that is connected to (or interacting with) the local hosted GATT server.
/// </summary>
public interface IPeripheral
{
    /// <summary>
    /// Gets the platform-specific identifier for this central's connection.
    /// </summary>
    string Uuid { get; }

    /// <summary>
    /// Gets the currently negotiated MTU (Maximum Transmission Unit) in bytes.
    /// </summary>
    int Mtu { get; }

    /// <summary>
    /// Gets or sets a custom object the host can associate with this central (e.g. session state).
    /// </summary>
    object? Context { get; set; }
}
