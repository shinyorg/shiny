namespace Shiny.Net.Wifi;


/// <summary>
/// The base for every failure this library raises. Catch this to handle any Wi-Fi problem.
/// </summary>
public class WifiException : Exception
{
    public WifiException(string message) : base(message) { }
    public WifiException(string message, Exception innerException) : base(message, innerException) { }
}


/// <summary>
/// Thrown when the operation cannot be performed on this platform at all.
/// </summary>
/// <remarks>
/// This is a statement about the OS, not about the device or its current state - retrying will
/// never help. The message names the specific limit (a missing Apple entitlement, an Android API
/// level that revoked the call, a platform with no such concept). Check the matching
/// <see cref="WifiCapabilities"/> flag first if you would rather branch than catch.
/// </remarks>
public class WifiNotSupportedException : WifiException
{
    public WifiNotSupportedException(string message) : base(message) { }


    internal static WifiNotSupportedException For(WifiCapabilities capability, string reason)
        => new($"{capability} is not available on this platform - {reason}");
}


/// <summary>
/// Thrown when the OS refused the operation because a permission, entitlement or manifest entry is
/// missing.
/// </summary>
/// <remarks>
/// Unlike <see cref="WifiNotSupportedException"/> this is fixable: the message names the exact
/// permission, entitlement or capability to add. It exists because most of these failures otherwise
/// look identical to "there are no networks here" - the scan succeeds and returns an empty list.
/// </remarks>
public class WifiPermissionException : WifiException
{
    public WifiPermissionException(string message) : base(message) { }
    public WifiPermissionException(string message, Exception innerException) : base(message, innerException) { }
}


/// <summary>
/// Thrown when a join was attempted but did not complete - wrong passphrase, network out of range,
/// user declined the system prompt, or association timed out.
/// </summary>
public class WifiConnectionException : WifiException
{
    public WifiConnectionException(string message) : base(message) { }
    public WifiConnectionException(string message, Exception innerException) : base(message, innerException) { }
}
