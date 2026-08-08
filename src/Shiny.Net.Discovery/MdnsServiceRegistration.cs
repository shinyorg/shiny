using System.Diagnostics.CodeAnalysis;

namespace Shiny.Net.Discovery;


/// <summary>
/// Describes a service this app wants to advertise on the local link.
/// </summary>
public sealed record MdnsServiceRegistration
{
    public MdnsServiceRegistration() { }

    /// <param name="instanceName">The human readable instance name, eg "Allan's Laptop".</param>
    /// <param name="serviceType">The DNS-SD service type, eg "_myapp._tcp".</param>
    /// <param name="port">The port the service is listening on.</param>
    [SetsRequiredMembers]
    public MdnsServiceRegistration(string instanceName, string serviceType, int port)
    {
        this.InstanceName = instanceName;
        this.ServiceType = serviceType;
        this.Port = port;
    }

    /// <summary>
    /// The human readable instance name. This is a single DNS-SD label, so it may contain
    /// spaces and UTF8 characters. If another device on the link already advertises this
    /// name, the platform renames it (typically by appending " (2)") - always read the
    /// final name back from <see cref="IMdnsPublication.InstanceName"/>.
    /// </summary>
    public required string InstanceName { get; init; }

    /// <summary>
    /// The DNS-SD service type, eg "_myapp._tcp". Custom types should be 1-15 characters,
    /// letters/digits/hyphens only, per RFC 6763 section 7.
    /// </summary>
    public required string ServiceType { get; init; }

    /// <summary>
    /// The port your service is actually listening on. You are responsible for opening the
    /// listener - publishing only advertises it.
    /// </summary>
    public required int Port { get; init; }

    /// <summary>
    /// The domain to publish in. Multicast DNS only supports "local", which is the default.
    /// </summary>
    public string Domain { get; init; } = MdnsConstants.LocalDomain;

    /// <summary>
    /// Optional key/value metadata published in the service TXT record. Keep the total
    /// encoded size small - RFC 6763 recommends staying under 1300 bytes so the record
    /// fits in a single packet.
    /// </summary>
    public IReadOnlyDictionary<string, string>? TxtRecords { get; init; }
}
