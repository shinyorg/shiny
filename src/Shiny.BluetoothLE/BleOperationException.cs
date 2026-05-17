namespace Shiny.BluetoothLE;


/// <summary>
/// Thrown when a GATT operation fails. Exposes the underlying platform GATT status code.
/// </summary>
public class BleOperationException : BleException
{
    /// <summary>
    /// Creates a new instance with the supplied message and GATT status code.
    /// </summary>
    /// <param name="message">The error description.</param>
    /// <param name="gattStatusCode">The platform-specific GATT status code returned by the stack.</param>
    public BleOperationException(string message, int gattStatusCode) : base(message)
    {
        this.GattStatusCode = gattStatusCode;
    }


    /// <summary>
    /// Gets the platform-specific GATT status code associated with the failure.
    /// </summary>
    public int GattStatusCode { get; }
}
