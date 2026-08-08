using System.Net;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Internals;
using Shiny.Net.Discovery.Managed.Dns;

namespace Shiny.Net.Discovery.Managed;


/// <summary>
/// The mDNS responder used everywhere there is no OS-level DNS-SD API to lean on - Windows,
/// Linux, macOS console apps and server .NET. It speaks the protocol directly on UDP 5353.
/// </summary>
sealed class ManagedMdnsManager : IMdnsManager, IDisposable
{
    /// <summary>How long a resolve waits before giving up.</summary>
    static readonly TimeSpan defaultResolveTimeout = TimeSpan.FromSeconds(5);

    readonly MulticastClient client;
    readonly ILogger logger;
    readonly DnsName hostName;


    public ManagedMdnsManager(ILogger<ManagedMdnsManager> logger)
    {
        this.logger = logger;
        this.client = new MulticastClient(logger);
        this.hostName = BuildHostName();
    }


    public IAsyncEnumerable<MdnsBrowseResult> Browse(MdnsBrowseConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        // parse up front so a bad service type throws at the call site rather than on first read
        var serviceType = ServiceTypeParser.Parse(config.ServiceType, config.Domain);
        var session = new MdnsBrowseSession(this.client, serviceType, config, this.logger);

        return session.Run(ct);
    }


    public async Task<MdnsService?> Resolve(
        string instanceName,
        string serviceType,
        TimeSpan? timeout = null,
        CancellationToken ct = default
    )
    {
        ServiceTypeParser.ValidateInstanceName(instanceName);
        var window = timeout ?? defaultResolveTimeout;

        var config = new MdnsBrowseConfig(serviceType) { ResolveTimeout = window };
        MdnsService? unresolved = null;

        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cancel.CancelAfter(window);
        try
        {
            await foreach (var result in this.Browse(config, cancel.Token).ConfigureAwait(false))
            {
                if (result.Status == MdnsBrowseStatus.Found &&
                    result.Service.InstanceName.Equals(instanceName, StringComparison.OrdinalIgnoreCase))
                {
                    if (result.Service.IsResolved)
                        return result.Service;

                    // hold onto it in case the endpoint never arrives - a caller that only
                    // wanted the TXT records still gets something back
                    unresolved = result.Service;
                }
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // the timeout closed the window
        }
        return unresolved;
    }


    public async Task<IMdnsPublication> Publish(MdnsServiceRegistration registration, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ServiceTypeParser.ValidateInstanceName(registration.InstanceName);

        if (registration.Port is <= 0 or > 65535)
            throw new MdnsException($"The port must be between 1 and 65535 but was {registration.Port}.");

        var serviceType = ServiceTypeParser.Parse(registration.ServiceType, registration.Domain);

        return await ManagedPublication
            .Create(this.client, this.logger, this.hostName, registration, serviceType, ct)
            .ConfigureAwait(false);
    }


    /// <summary>
    /// Builds the host name our SRV records target. The "-shiny" suffix deliberately keeps us
    /// off "&lt;machine&gt;.local", which the OS responder (avahi-daemon, mDNSResponder, the
    /// Windows DNS client) already owns - fighting it over the same name causes cache flapping.
    /// </summary>
    static DnsName BuildHostName()
    {
        string raw;
        try
        {
            raw = System.Net.Dns.GetHostName();
        }
        catch (Exception)
        {
            raw = Environment.MachineName;
        }

        var label = raw.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "shiny";
        var sanitized = new string(
            label
                .Select(x => Char.IsAsciiLetterOrDigit(x) || x == '-' ? Char.ToLowerInvariant(x) : '-')
                .ToArray()
        ).Trim('-');

        if (sanitized.Length == 0)
            sanitized = "shiny";

        if (sanitized.Length > 40)
            sanitized = sanitized[..40];

        return new DnsName($"{sanitized}-shiny", MdnsConstants.LocalDomain);
    }


    public void Dispose() => this.client.Dispose();
}
