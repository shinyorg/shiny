namespace Shiny.Bluetooth;


/// <summary>
/// The base for every failure this library raises. Catch this to handle any classic Bluetooth problem.
/// </summary>
public class BluetoothException : Exception
{
    public BluetoothException(string message) : base(message) { }
    public BluetoothException(string message, Exception innerException) : base(message, innerException) { }
}


/// <summary>
/// Thrown when the operation cannot be performed on this platform at all.
/// </summary>
/// <remarks>
/// A statement about the OS, not the device or its state - retrying will never help. Check the matching
/// <see cref="BluetoothCapabilities"/> flag first if you would rather branch than catch.
/// </remarks>
public class BluetoothNotSupportedException : BluetoothException
{
    public BluetoothNotSupportedException(string message) : base(message) { }


    internal static BluetoothNotSupportedException For(BluetoothCapabilities capability, string reason)
        => new($"{capability} is not available on this platform - {reason}");
}


/// <summary>
/// Thrown when the OS refused the operation because a permission or manifest entry is missing.
/// The message names what to add or grant.
/// </summary>
public class BluetoothPermissionException : BluetoothException
{
    public BluetoothPermissionException(string message) : base(message) { }
}


/// <summary>
/// Thrown when an RFCOMM connection could not be opened - the device is out of range or off, does
/// not publish the requested service, or refused the connection.
/// </summary>
public class BluetoothConnectionException : BluetoothException
{
    public BluetoothConnectionException(string message) : base(message) { }
    public BluetoothConnectionException(string message, Exception innerException) : base(message, innerException) { }
}
