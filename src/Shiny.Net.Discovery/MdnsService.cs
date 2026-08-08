using System.Net;

namespace Shiny.Net.Discovery;


/// <summary>
/// A DNS-SD service instance seen on the local link.
/// </summary>
public sealed record MdnsService
{
    /// <summary>
    /// The human readable instance name, eg "Allan's Printer". This is the unescaped
    /// label - it may contain spaces, dots and other UTF8 characters.
    /// </summary>
    public required string InstanceName { get; init; }

    /// <summary>
    /// The service type in DNS-SD form, eg "_http._tcp".
    /// </summary>
    public required string ServiceType { get; init; }

    /// <summary>
    /// The domain the service was discovered in. Almost always "local".
    /// </summary>
    public string Domain { get; init; } = MdnsConstants.LocalDomain;

    /// <summary>
    /// The target host name from the SRV record, eg "printer.local". Null when the
    /// service has not been resolved yet.
    /// </summary>
    public string? HostName { get; init; }

    /// <summary>
    /// The TCP/UDP port from the SRV record. Zero when the service has not been resolved yet.
    /// </summary>
    public int Port { get; init; }

    /// <summary>
    /// The IP addresses the host name resolved to. Empty when the service has not been resolved yet.
    /// A single service commonly resolves to both IPv4 and IPv6 addresses.
    /// </summary>
    public IReadOnlyList<IPAddress> Addresses { get; init; } = Array.Empty<IPAddress>();

    /// <summary>
    /// The key/value pairs from the service TXT record. Keys are case-insensitive per RFC 6763.
    /// A key present with no value is represented as an empty string.
    /// </summary>
    public IReadOnlyDictionary<string, string> TxtRecords { get; init; } = MdnsConstants.EmptyTxt;

    /// <summary>
    /// The fully qualified DNS-SD name, eg "Allan's Printer._http._tcp.local".
    /// </summary>
    public string FullName => $"{this.InstanceName}.{this.ServiceType}.{this.Domain}";

    /// <summary>
    /// True when the service has an endpoint that can actually be connected to.
    /// </summary>
    public bool IsResolved => this.Port > 0 && this.Addresses.Count > 0;

    /// <summary>
    /// Builds a connectable endpoint from the first address of the requested family, or the
    /// first address of any family when <paramref name="family"/> is null.
    /// </summary>
    /// <param name="family">Optionally restrict to InterNetwork (IPv4) or InterNetworkV6.</param>
    /// <returns>The endpoint, or null when the service is unresolved or has no matching address.</returns>
    public IPEndPoint? GetEndPoint(System.Net.Sockets.AddressFamily? family = null)
    {
        if (this.Port <= 0)
            return null;

        var address = family == null
            ? this.Addresses.FirstOrDefault()
            : this.Addresses.FirstOrDefault(x => x.AddressFamily == family);

        return address == null ? null : new IPEndPoint(address, this.Port);
    }

    public override string ToString()
        => $"{this.FullName} => {this.HostName}:{this.Port} ({this.Addresses.Count} address(es))";
}
