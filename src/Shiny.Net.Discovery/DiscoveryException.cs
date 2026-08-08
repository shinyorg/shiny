namespace Shiny.Net.Discovery;


/// <summary>
/// The base for every failure this library raises. Catch this to handle any discovery problem
/// regardless of protocol.
/// </summary>
public class DiscoveryException : Exception
{
    public DiscoveryException(string message) : base(message) { }
    public DiscoveryException(string message, Exception innerException) : base(message, innerException) { }
}


/// <summary>
/// Thrown when an SSDP/UPnP operation fails - a malformed search target, a device description
/// that could not be fetched or parsed, or a rejected advertisement.
/// </summary>
public class SsdpException : DiscoveryException
{
    public SsdpException(string message) : base(message) { }
    public SsdpException(string message, Exception innerException) : base(message, innerException) { }
}


/// <summary>
/// Thrown when a WS-Discovery operation fails - an unparseable envelope, an invalid probe, or a
/// rejected advertisement.
/// </summary>
public class WsDiscoveryException : DiscoveryException
{
    public WsDiscoveryException(string message) : base(message) { }
    public WsDiscoveryException(string message, Exception innerException) : base(message, innerException) { }
}


/// <summary>
/// Thrown when the operating system refused the multicast operation because the app is missing a
/// platform permission or entitlement.
/// </summary>
/// <remarks>
/// This exists because every one of these failures otherwise looks identical to "there is nothing
/// on the network" - the socket call fails, or succeeds and then silently receives nothing. The
/// message names the specific manifest entry, entitlement or runtime permission that is missing.
/// </remarks>
public class DiscoveryPermissionException : DiscoveryException
{
    public DiscoveryPermissionException(string message) : base(message) { }
    public DiscoveryPermissionException(string message, Exception innerException) : base(message, innerException) { }
}
