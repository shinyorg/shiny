using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Managed.Multicast;

namespace Shiny.Net.Discovery.Managed.WsDiscovery;


/// <summary>
/// The WS-Discovery implementation. Managed on every platform, because no OS exposes a
/// WS-Discovery API to an app.
/// </summary>
sealed class ManagedWsDiscoveryManager : IWsDiscoveryManager, IDisposable
{
    static readonly TimeSpan defaultResolveTimeout = TimeSpan.FromSeconds(5);

    readonly MulticastSocketSet sockets;
    readonly ILogger logger;


    public ManagedWsDiscoveryManager(ILogger<ManagedWsDiscoveryManager> logger)
    {
        this.logger = logger;
        this.sockets = new MulticastSocketSet(WsdTransport.Endpoint, logger);
    }


    public IAsyncEnumerable<WsDiscoveryResult> Browse(WsdProbeConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.Profiles == WsDiscoveryProfile.None)
            throw new WsDiscoveryException("At least one WS-Discovery profile must be selected.");

        var session = new WsdProbeSession(this.sockets, config, this.logger);
        return session.Run(ct);
    }


    public async Task<WsdTarget?> Resolve(
        string endpointReference,
        TimeSpan? timeout = null,
        CancellationToken ct = default
    )
    {
        var epr = WsdEnvelopeReader.NormalizeEndpointReference(endpointReference ?? String.Empty);
        if (String.IsNullOrWhiteSpace(epr))
            throw new WsDiscoveryException("An endpoint reference is required.");

        var window = timeout ?? defaultResolveTimeout;

        // a Resolve is a targeted Probe as far as this session is concerned - the browse already
        // sends one for any target that turns up without an address
        var config = new WsdProbeConfig { ProbeInterval = window };

        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cancel.CancelAfter(window);

        try
        {
            await foreach (var result in this.Browse(config, cancel.Token).ConfigureAwait(false))
            {
                if (result.Status == WsDiscoveryStatus.Found &&
                    result.Target.EndpointReference.Equals(epr, StringComparison.OrdinalIgnoreCase))
                {
                    return result.Target;
                }
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // the timeout closed the window - plenty of devices simply never answer
        }
        return null;
    }


    public Task<IWsDiscoveryPublication> Publish(WsDiscoveryRegistration registration, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        return WsdPublication.Create(this.sockets, registration, this.logger, ct);
    }


    public void Dispose() => this.sockets.Dispose();
}
