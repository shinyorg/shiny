using System.Xml;

namespace Shiny.Net.Discovery;


public static class WsDiscoveryExtensions
{
    extension(IWsDiscoveryManager manager)
    {
        /// <summary>
        /// Browses for every WS-Discovery target on the network.
        /// </summary>
        /// <param name="ct">Cancel to stop browsing.</param>
        public IAsyncEnumerable<WsDiscoveryResult> Browse(CancellationToken ct = default)
            => manager.Browse(new WsdProbeConfig(), ct);

        /// <summary>
        /// Browses for targets implementing specific types.
        /// </summary>
        /// <param name="types">The types a device must implement.</param>
        /// <param name="ct">Cancel to stop browsing.</param>
        public IAsyncEnumerable<WsDiscoveryResult> Browse(XmlQualifiedName[] types, CancellationToken ct = default)
            => manager.Browse(new WsdProbeConfig(types), ct);

        /// <summary>
        /// Probes for a fixed window and returns everything that answered. Use this for one-shot
        /// scans; use <see cref="IWsDiscoveryManager.Browse(WsdProbeConfig, CancellationToken)"/>
        /// for a live list.
        /// </summary>
        /// <param name="config">What to probe for. Defaults to everything.</param>
        /// <param name="scanTime">
        /// How long to collect for. Defaults to 5 seconds - cameras are slow, and much less than
        /// that will miss them.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<IReadOnlyList<WsdTarget>> Probe(
            WsdProbeConfig? config = null,
            TimeSpan? scanTime = null,
            CancellationToken ct = default
        )
        {
            var found = new Dictionary<string, WsdTarget>(StringComparer.OrdinalIgnoreCase);
            using var cancel = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cancel.CancelAfter(scanTime ?? TimeSpan.FromSeconds(5));

            try
            {
                await foreach (var result in manager.Browse(config ?? new WsdProbeConfig(), cancel.Token).ConfigureAwait(false))
                {
                    if (result.Status == WsDiscoveryStatus.Found)
                        found[result.Target.EndpointReference] = result.Target;
                    else
                        found.Remove(result.Target.EndpointReference);
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // the scan window closed - that is the success path here
            }
            return found.Values.ToList();
        }

        /// <summary>
        /// Finds ONVIF cameras.
        /// </summary>
        /// <remarks>
        /// Deliberately probes with no type filter and matches locally on either ONVIF type. A
        /// typed probe is what the ONVIF specification describes, but in practice it finds fewer
        /// cameras - some only answer the older NetworkVideoTransmitter type, some only the newer
        /// Device type, and some answer an untyped probe while ignoring both.
        /// </remarks>
        /// <param name="scanTime">How long to collect for. Defaults to 5 seconds.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<IReadOnlyList<WsdTarget>> ProbeOnvifCameras(
            TimeSpan? scanTime = null,
            CancellationToken ct = default
        )
        {
            var all = await manager.Probe(new WsdProbeConfig(), scanTime, ct).ConfigureAwait(false);

            return all
                .Where(x =>
                    x.Types.Contains(WsDiscoveryConstants.OnvifNetworkVideoTransmitter) ||
                    x.Types.Contains(WsDiscoveryConstants.OnvifDevice)
                )
                .ToList();
        }
    }
}
