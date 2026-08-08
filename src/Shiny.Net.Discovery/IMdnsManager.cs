namespace Shiny.Net.Discovery;


/// <summary>
/// Browses, resolves and publishes DNS-SD (Bonjour/Zeroconf) services over multicast DNS
/// on the local link.
/// </summary>
/// <remarks>
/// Backed by NsdManager on Android and NSNetService on Apple platforms, which means no
/// multicast entitlement is needed on iOS. Everywhere else (Windows, Linux, macOS console,
/// server .NET) a managed responder speaks mDNS directly on UDP 5353.
/// </remarks>
public interface IMdnsManager
{
    /// <summary>
    /// Continuously browses the local link for services of a type. The enumerable never
    /// completes on its own - it runs until the token is cancelled or you stop enumerating.
    /// </summary>
    /// <param name="config">The service type to browse for plus resolution options.</param>
    /// <param name="ct">Cancel to stop browsing.</param>
    /// <returns>A stream of appear/disappear changes.</returns>
    /// <example>
    /// <code>
    /// await foreach (var result in mdns.Browse("_http._tcp", ct))
    ///     Console.WriteLine($"{result.Status}: {result.Service}");
    /// </code>
    /// </example>
    IAsyncEnumerable<MdnsBrowseResult> Browse(MdnsBrowseConfig config, CancellationToken ct = default);

    /// <summary>
    /// Resolves a single known service instance to its host, port, addresses and TXT records.
    /// </summary>
    /// <param name="instanceName">The instance name, eg "Allan's Printer".</param>
    /// <param name="serviceType">The service type, eg "_http._tcp".</param>
    /// <param name="timeout">How long to wait before giving up. Defaults to 5 seconds.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resolved service, or null if it did not answer within the timeout.</returns>
    Task<MdnsService?> Resolve(string instanceName, string serviceType, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Advertises a service on the local link so other devices can discover it. You are
    /// responsible for having something listening on the port - this only advertises it.
    /// </summary>
    /// <param name="registration">What to advertise.</param>
    /// <param name="ct">Cancellation token covering the probe/announce handshake.</param>
    /// <returns>
    /// A handle carrying the final (possibly renamed) instance name. Dispose it to send a
    /// goodbye packet and stop advertising.
    /// </returns>
    Task<IMdnsPublication> Publish(MdnsServiceRegistration registration, CancellationToken ct = default);
}
