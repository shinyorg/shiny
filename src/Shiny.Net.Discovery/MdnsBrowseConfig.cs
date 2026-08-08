using System.Diagnostics.CodeAnalysis;

namespace Shiny.Net.Discovery;


/// <summary>
/// Options for a browse operation.
/// </summary>
public sealed record MdnsBrowseConfig
{
    public MdnsBrowseConfig() { }

    /// <param name="serviceType">The DNS-SD service type, eg "_http._tcp".</param>
    [SetsRequiredMembers]
    public MdnsBrowseConfig(string serviceType)
        => this.ServiceType = serviceType;

    /// <summary>
    /// The DNS-SD service type to browse for, eg "_http._tcp" or "_ipp._tcp". The leading
    /// underscores and the transport label are required; a trailing ".local" is optional.
    /// </summary>
    public required string ServiceType { get; init; }

    /// <summary>
    /// The domain to browse. Multicast DNS only supports "local", which is the default.
    /// </summary>
    public string Domain { get; init; } = MdnsConstants.LocalDomain;

    /// <summary>
    /// When true (the default), each discovered instance is resolved to its host, port,
    /// addresses and TXT records before being emitted. Set to false to get bare instance
    /// names as fast as possible and resolve them yourself on demand.
    /// </summary>
    public bool ResolveServices { get; init; } = true;

    /// <summary>
    /// How long to wait for a single instance to resolve before emitting it unresolved.
    /// Ignored when <see cref="ResolveServices"/> is false.
    /// </summary>
    public TimeSpan ResolveTimeout { get; init; } = TimeSpan.FromSeconds(5);
}
