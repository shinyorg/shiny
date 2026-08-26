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
    /// Gets the maximum number of bytes that fit in a single GATT operation to this central - the negotiated
    /// ATT MTU minus the 3-byte ATT header. This is the size to cap notification and read payloads at; do NOT
    /// subtract the header again. Reports 20 until the central negotiates a larger MTU.
    /// </summary>
    int Mtu { get; }

    /// <summary>
    /// Gets or sets a custom object the host can associate with this central (e.g. session state).
    /// </summary>
    object? Context { get; set; }
}
