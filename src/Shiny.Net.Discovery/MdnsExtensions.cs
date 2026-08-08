using System.Diagnostics;

namespace Shiny.Net.Discovery;


public static class MdnsExtensions
{
    extension(IMdnsManager manager)
    {
        /// <summary>
        /// Browses for a service type using the default resolution options.
        /// </summary>
        /// <param name="serviceType">The DNS-SD service type, eg "_http._tcp".</param>
        /// <param name="ct">Cancel to stop browsing.</param>
        public IAsyncEnumerable<MdnsBrowseResult> Browse(string serviceType, CancellationToken ct = default)
            => manager.Browse(new MdnsBrowseConfig(serviceType), ct);

        /// <summary>
        /// Browses for a fixed window and returns everything that was still present when the
        /// window closed. Use this for one-shot "show me what's on the network" scans; use
        /// <see cref="IMdnsManager.Browse(MdnsBrowseConfig, CancellationToken)"/> when you want
        /// a live list.
        /// </summary>
        /// <param name="serviceType">The DNS-SD service type, eg "_http._tcp".</param>
        /// <param name="scanTime">How long to collect for. Defaults to 5 seconds.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<IReadOnlyList<MdnsService>> BrowseOnce(
            string serviceType,
            TimeSpan? scanTime = null,
            CancellationToken ct = default
        )
        {
            var found = new Dictionary<string, MdnsService>(StringComparer.OrdinalIgnoreCase);
            using var cancel = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cancel.CancelAfter(scanTime ?? TimeSpan.FromSeconds(5));

            try
            {
                await foreach (var result in manager.Browse(serviceType, cancel.Token).ConfigureAwait(false))
                {
                    if (result.Status == MdnsBrowseStatus.Found)
                        found[result.Service.FullName] = result.Service;
                    else
                        found.Remove(result.Service.FullName);
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // the scan window closed - that is the success path here
            }
            return found.Values.ToList();
        }

        /// <summary>
        /// Advertises a service without TXT metadata.
        /// </summary>
        /// <param name="instanceName">The human readable instance name.</param>
        /// <param name="serviceType">The DNS-SD service type, eg "_myapp._tcp".</param>
        /// <param name="port">The port your listener is bound to.</param>
        /// <param name="ct">Cancellation token.</param>
        public Task<IMdnsPublication> Publish(string instanceName, string serviceType, int port, CancellationToken ct = default)
            => manager.Publish(new MdnsServiceRegistration(instanceName, serviceType, port), ct);
    }


    extension(MdnsService service)
    {
        /// <summary>
        /// Reads a TXT record value, returning null when the key is absent.
        /// </summary>
        public string? GetTxt(string key)
            => service.TxtRecords.TryGetValue(key, out var value) ? value : null;

        /// <summary>
        /// Reads a TXT record value and converts it, returning <paramref name="defaultValue"/>
        /// when the key is absent or cannot be converted.
        /// </summary>
        public T? GetTxt<T>(string key, T? defaultValue = default) where T : IParsable<T>
        {
            if (!service.TxtRecords.TryGetValue(key, out var raw))
                return defaultValue;

            return T.TryParse(raw, null, out var parsed) ? parsed : defaultValue;
        }
    }
}
