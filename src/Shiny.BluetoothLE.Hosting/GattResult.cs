using System;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Result returned from a hosted characteristic read handler.
/// </summary>
/// <param name="Status">The GATT status to send back to the central.</param>
/// <param name="Data">The payload to return on success, or null for an error.</param>
public record GattResult(
    GattState Status,
    byte[]? Data
)
{
    /// <summary>
    /// Creates a successful read result containing the supplied data.
    /// </summary>
    /// <param name="data">The bytes to return to the central.</param>
    /// <returns>A success result.</returns>
    public static GattResult Success(byte[] data)
        => new GattResult(GattState.Success, data);

    /// <summary>
    /// Creates an error result with the supplied GATT status and no data.
    /// </summary>
    /// <param name="status">The error status to return.</param>
    /// <returns>An error result.</returns>
    public static GattResult Error(GattState status) => new GattResult(status, null);
}
