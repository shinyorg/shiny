namespace Shiny.Net.Discovery;


public static class SsdpExtensions
{
    extension(ISsdpManager manager)
    {
        /// <summary>
        /// Browses for a search target using the default options.
        /// </summary>
        /// <param name="searchTarget">
        /// <see cref="SsdpConstants.SearchAll"/>, <see cref="SsdpConstants.RootDevice"/>, a
        /// "uuid:..." UDN, or a device/service type URN.
        /// </param>
        /// <param name="ct">Cancel to stop browsing.</param>
        public IAsyncEnumerable<SsdpBrowseResult> Browse(string searchTarget, CancellationToken ct = default)
            => manager.Browse(new SsdpBrowseConfig(searchTarget), ct);

        /// <summary>
        /// Searches for a fixed window and returns everything that was still present when the
        /// window closed. Use this for one-shot "what is on the network" scans; use
        /// <see cref="ISsdpManager.Browse(SsdpBrowseConfig, CancellationToken)"/> for a live list.
        /// </summary>
        /// <param name="searchTarget">What to search for.</param>
        /// <param name="scanTime">How long to collect for. Defaults to 5 seconds.</param>
        /// <param name="ct">Cancellation token.</param>
        public Task<IReadOnlyList<SsdpDevice>> Search(
            string searchTarget,
            TimeSpan? scanTime = null,
            CancellationToken ct = default
        )
            => manager.Search(new SsdpBrowseConfig(searchTarget), scanTime, ct);

        /// <summary>
        /// Searches for every device on the network for a fixed window.
        /// </summary>
        /// <param name="scanTime">How long to collect for. Defaults to 5 seconds.</param>
        /// <param name="ct">Cancellation token.</param>
        public Task<IReadOnlyList<SsdpDevice>> SearchAll(TimeSpan? scanTime = null, CancellationToken ct = default)
            => manager.Search(new SsdpBrowseConfig(SsdpConstants.SearchAll), scanTime, ct);

        /// <summary>
        /// Searches for a fixed window using full browse options and returns everything that was
        /// still present when the window closed.
        /// </summary>
        /// <param name="config">What to search for and how.</param>
        /// <param name="scanTime">How long to collect for. Defaults to 5 seconds.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<IReadOnlyList<SsdpDevice>> Search(
            SsdpBrowseConfig config,
            TimeSpan? scanTime = null,
            CancellationToken ct = default
        )
        {
            var found = new Dictionary<string, SsdpDevice>(StringComparer.OrdinalIgnoreCase);
            using var cancel = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cancel.CancelAfter(scanTime ?? TimeSpan.FromSeconds(5));

            try
            {
                await foreach (var result in manager.Browse(config, cancel.Token).ConfigureAwait(false))
                {
                    if (result.Status == SsdpBrowseStatus.Found)
                        found[result.Device.Udn] = result.Device;
                    else
                        found.Remove(result.Device.Udn);
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // the scan window closed - that is the success path here
            }
            return found.Values.ToList();
        }

        /// <summary>
        /// Advertises a root device without a device or service type.
        /// </summary>
        /// <param name="udn">The device's unique name. A bare GUID is accepted and prefixed.</param>
        /// <param name="location">The URL your device description document is served from.</param>
        /// <param name="ct">Cancellation token.</param>
        public Task<ISsdpPublication> Publish(string udn, Uri location, CancellationToken ct = default)
            => manager.Publish(new SsdpDeviceRegistration(udn, location), ct);
    }
}
