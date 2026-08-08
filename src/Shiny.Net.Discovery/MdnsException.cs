namespace Shiny.Net.Discovery;


/// <summary>
/// Thrown when an mDNS operation fails - a malformed service type, a publish rejected by the
/// platform responder, or a name conflict that could not be resolved.
/// </summary>
public class MdnsException : DiscoveryException
{
    public MdnsException(string message) : base(message) { }
    public MdnsException(string message, Exception innerException) : base(message, innerException) { }
}
