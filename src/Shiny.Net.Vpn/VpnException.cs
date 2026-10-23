namespace Shiny.Net.Vpn;


/// <summary>
/// The base for every failure this library raises. Catch this to handle any VPN problem.
/// </summary>
public class VpnException : Exception
{
    public VpnException(string message) : base(message) { }
    public VpnException(string message, Exception innerException) : base(message, innerException) { }
}


/// <summary>
/// Thrown when the operation or profile option cannot be used on this platform at all.
/// </summary>
/// <remarks>
/// A statement about the OS, not the device or its state - retrying never helps. Check the matching
/// <see cref="VpnCapabilities"/> flag first if you would rather branch than catch.
/// </remarks>
public class VpnNotSupportedException : VpnException
{
    public VpnNotSupportedException(string message) : base(message) { }


    internal static VpnNotSupportedException For(VpnCapabilities capability, string reason)
        => new($"{capability} is not available on this platform - {reason}");
}


/// <summary>
/// Thrown when the OS refused because the user has not allowed this app to manage a VPN, or an
/// entitlement or policy is missing.
/// </summary>
/// <remarks>
/// Fixable: the message names what is missing. A user who declined the consent prompt surfaces from
/// <see cref="IVpnManager.SaveProfile"/> as <see cref="AccessState.Denied"/> instead - this is for the
/// calls made afterwards without that consent.
/// </remarks>
public class VpnPermissionException : VpnException
{
    public VpnPermissionException(string message) : base(message) { }
    public VpnPermissionException(string message, Exception innerException) : base(message, innerException) { }
}


/// <summary>
/// Thrown when a tunnel was started but did not come up - authentication failed, the server did not
/// answer, the certificate was rejected, or it timed out.
/// </summary>
public class VpnConnectionException : VpnException
{
    public VpnConnectionException(string message) : base(message) { }
    public VpnConnectionException(string message, Exception innerException) : base(message, innerException) { }
}
