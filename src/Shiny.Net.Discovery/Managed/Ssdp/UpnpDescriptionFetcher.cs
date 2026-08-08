using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Managed.Multicast;

namespace Shiny.Net.Discovery.Managed.Ssdp;


/// <summary>
/// Fetches a device description document, subject to a fetch policy and a size cap.
/// </summary>
/// <remarks>
/// A LOCATION is an unauthenticated URL handed to us by anyone who can reach the multicast group,
/// and fetching it is a request this process makes on their behalf. The default policy demands the
/// URL point back at the address the advertisement came from; the size cap and timeout stop a
/// device that accepts the connection and then trickles bytes forever.
/// </remarks>
sealed class UpnpDescriptionFetcher(IHttpClientFactory httpClientFactory, ILogger logger)
{
    /// <summary>An ssdp:all sweep of a busy network would otherwise open hundreds of sockets at once.</summary>
    static readonly SemaphoreSlim concurrency = new(4, 4);

    static readonly TimeSpan fetchTimeout = TimeSpan.FromSeconds(10);


    public async Task<UpnpDeviceDescription> Fetch(
        SsdpDevice device,
        Func<Uri, IPAddress, bool>? filter,
        CancellationToken ct
    )
    {
        var location = device.Location
            ?? throw new SsdpException($"The device {device.Udn} has no LOCATION to fetch a description from.");

        var source = device.SourceAddress
            ?? throw new SsdpException($"The device {device.Udn} has no known source address, so its description URL cannot be verified.");

        if (filter != null)
        {
            if (!filter(location, source))
                throw new SsdpException($"The description URL {location} for {device.Udn} was refused by the configured DescriptionUrlFilter.");
        }
        else if (!LocalNetworkGuard.IsFetchable(location, source, out var reason))
        {
            throw new SsdpException(
                $"Refusing to fetch the description URL {location} for {device.Udn} because {reason}. " +
                "Set SsdpBrowseConfig.DescriptionUrlFilter if this device is legitimate on your network."
            );
        }

        await concurrency.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            logger.LogDebug("Fetching the UPnP description for {Udn} from {Location}", device.Udn, location);

            var data = await this.Download(location, ct).ConfigureAwait(false);
            return UpnpDescriptionParser.Parse(data, location);
        }
        finally
        {
            concurrency.Release();
        }
    }


    async Task<byte[]> Download(Uri location, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(fetchTimeout);

        var client = httpClientFactory.CreateClient(SsdpConstants.HttpClientName);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, location);

            // headers first so the size cap applies while the body streams, rather than after
            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new SsdpException($"The device description at {location} returned HTTP {(int)response.StatusCode}.");

            // deliberately not checking Content-Type - devices return text/html,
            // application/octet-stream or nothing at all for a perfectly good description
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            return await ReadCapped(stream, location, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new SsdpException($"The device description at {location} did not arrive within {fetchTimeout.TotalSeconds:N0}s.");
        }
        catch (HttpRequestException ex)
        {
            throw new SsdpException($"Could not fetch the device description at {location}.", ex);
        }
    }


    /// <summary>
    /// Reads the body, refusing to grow past the cap. Content-Length is not trusted - devices
    /// omit it, lie about it, or use chunked encoding.
    /// </summary>
    static async Task<byte[]> ReadCapped(Stream stream, Uri location, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];

        while (true)
        {
            var read = await stream.ReadAsync(chunk, ct).ConfigureAwait(false);
            if (read == 0)
                break;

            if (buffer.Length + read > UpnpDescriptionParser.MaxDocumentBytes)
                throw new SsdpException($"The device description at {location} exceeds the {UpnpDescriptionParser.MaxDocumentBytes / 1024}KB limit.");

            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }
}
