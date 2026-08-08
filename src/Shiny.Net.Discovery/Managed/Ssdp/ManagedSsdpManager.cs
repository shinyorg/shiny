using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Managed.Multicast;

namespace Shiny.Net.Discovery.Managed.Ssdp;


/// <summary>
/// The SSDP implementation. Managed on every platform, because no OS exposes an SSDP API - both
/// Android's NsdManager and Apple's Bonjour stack are strictly mDNS/DNS-SD.
/// </summary>
sealed class ManagedSsdpManager : ISsdpManager, IDisposable
{
    readonly MulticastSocketSet sockets;
    readonly UpnpDescriptionFetcher fetcher;
    readonly ILogger logger;


    public ManagedSsdpManager(ILogger<ManagedSsdpManager> logger, IHttpClientFactory httpClientFactory)
    {
        this.logger = logger;
        this.sockets = new MulticastSocketSet(SsdpTransport.Endpoint, logger);
        this.fetcher = new UpnpDescriptionFetcher(httpClientFactory, logger);
    }


    public IAsyncEnumerable<SsdpBrowseResult> Browse(SsdpBrowseConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (String.IsNullOrWhiteSpace(config.SearchTarget))
            throw new SsdpException("The search target is required. Use SsdpConstants.SearchAll to find everything.");

        var session = new SsdpBrowseSession(this.sockets, config, this.logger);
        return session.Run(ct);
    }


    public Task<UpnpDeviceDescription> GetDescription(
        SsdpDevice device,
        Func<Uri, IPAddress, bool>? urlFilter = null,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(device);
        return this.fetcher.Fetch(device, urlFilter, ct);
    }


    public Task<ISsdpPublication> Publish(SsdpDeviceRegistration registration, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        return SsdpPublication.Create(this.sockets, registration, this.logger, ct);
    }


    public void Dispose() => this.sockets.Dispose();
}
