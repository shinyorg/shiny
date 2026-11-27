using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Android.Content;
using Android.Net.Nsd;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Internals;
using Shiny.Net.Discovery.Managed;
using Application = Android.App.Application;

// the managed mDNS fallback for the pre-API 34 resolve holds a WifiManager.MulticastLock while it
// queries; without this the lock throws and nothing is received over Wi-Fi. Normal permission, no prompt.
[assembly: UsesPermission("android.permission.CHANGE_WIFI_MULTICAST_STATE")]

namespace Shiny.Net.Discovery;


/// <summary>
/// Browses and publishes through NsdManager, the system DNS-SD service, which keeps the multicast
/// path alive for us. The one exception is below API 34: while NsdManager's single resolve slot is
/// held by a resolve that timed out, resolves go through the managed mDNS stack, which holds a
/// WifiManager.MulticastLock (CHANGE_WIFI_MULTICAST_STATE, declared by this package) while it queries.
/// </summary>
public class AndroidMdnsManager(ILogger<AndroidMdnsManager> logger) : IMdnsManager
{
    static readonly TimeSpan defaultResolveTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Pre-API 34 NsdManager only tolerates one resolve in flight (a second comes back as
    /// FailureAlreadyActive) and cannot cancel it, so the slot is only free once the outstanding
    /// resolve's listener reports back - not when the caller's timeout runs out.
    /// </summary>
    readonly LegacyResolveSlot legacySlot = new();

    /// <summary>
    /// Only built when a pre-API 34 resolve finds the slot orphaned. Its sockets (and the multicast
    /// lock) are only open while a fallback resolve runs.
    /// </summary>
    readonly Lazy<ManagedMdnsManager> managed = new(() => new ManagedMdnsManager(logger));

    NsdManager Native => (NsdManager)Application.Context.GetSystemService(Context.NsdService)!;


    public async IAsyncEnumerable<MdnsBrowseResult> Browse(
        MdnsBrowseConfig config,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        ArgumentNullException.ThrowIfNull(config);
        var serviceType = ServiceTypeParser.Parse(config.ServiceType, config.Domain);

        var channel = Channel.CreateUnbounded<MdnsBrowseResult>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        );

        // Bumped on every found and lost for a name. A resolve that finishes after its instance was lost
        // (or found again) is stale, and emitting it would resurrect a service the caller saw go away.
        var generations = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var listener = new NsdDiscoveryListener(
            found =>
            {
                var generation = generations.AddOrUpdate(found.ServiceName ?? String.Empty, 1, (_, x) => x + 1);
                _ = this.OnServiceFound(found, serviceType, config, channel, generations, generation, ct);
            },
            lost =>
            {
                generations.AddOrUpdate(lost.ServiceName ?? String.Empty, 1, (_, x) => x + 1);
                channel.Writer.TryWrite(new MdnsBrowseResult(
                    MdnsBrowseStatus.Lost,
                    new MdnsService
                    {
                        InstanceName = lost.ServiceName ?? String.Empty,
                        ServiceType = serviceType.ServiceType,
                        Domain = serviceType.Domain
                    }
                ));
            },
            (type, error) => channel.Writer.TryComplete(new MdnsException(
                $"NsdManager refused to browse '{type}' - {error}"
            ))
        );

        var nsd = this.Native;
        nsd.DiscoverServices(serviceType.ServiceType, NsdProtocol.DnsSd, listener);
        try
        {
            await foreach (var result in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                yield return result;
        }
        finally
        {
            try
            {
                nsd.StopServiceDiscovery(listener);
            }
            catch (Exception ex)
            {
                // throws if discovery already stopped itself - nothing left to clean up
                logger.LogDebug(ex, "NsdManager rejected StopServiceDiscovery for {ServiceType}", serviceType.ServiceType);
            }
        }
    }


    async Task OnServiceFound(
        NsdServiceInfo info,
        ServiceTypeInfo serviceType,
        MdnsBrowseConfig config,
        Channel<MdnsBrowseResult> channel,
        ConcurrentDictionary<string, int> generations,
        int generation,
        CancellationToken ct
    )
    {
        try
        {
            var final = config.ResolveServices
                ? await this.ResolveNative(info, serviceType, config.ResolveTimeout, ct).ConfigureAwait(false) ?? Convert(info, serviceType)
                : Convert(info, serviceType);

            var isCurrent = generations.TryGetValue(info.ServiceName ?? String.Empty, out var latest) && latest == generation;
            if (isCurrent)
                channel.Writer.TryWrite(new MdnsBrowseResult(MdnsBrowseStatus.Found, final));
            else
                logger.LogDebug("Dropped a stale resolve of {Name} - it was lost or found again while resolving", info.ServiceName);
        }
        catch (OperationCanceledException)
        {
            // browse stopped while this instance was resolving
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to handle discovery of {Name}", info.ServiceName);
        }
    }


    public async Task<MdnsService?> Resolve(
        string instanceName,
        string serviceType,
        TimeSpan? timeout = null,
        CancellationToken ct = default
    )
    {
        ServiceTypeParser.ValidateInstanceName(instanceName);
        var info = ServiceTypeParser.Parse(serviceType);

        var request = new NsdServiceInfo
        {
            ServiceName = instanceName,
            ServiceType = info.ServiceType
        };

        return await this
            .ResolveNative(request, info, timeout ?? defaultResolveTimeout, ct)
            .ConfigureAwait(false);
    }


    async Task<MdnsService?> ResolveNative(NsdServiceInfo info, ServiceTypeInfo serviceType, TimeSpan timeout, CancellationToken ct)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            var resolved = await this.ResolveViaCallback(info, timeout, ct).ConfigureAwait(false);
            return resolved == null ? null : Convert(resolved, serviceType);
        }
        return await this.ResolveViaListener(info, serviceType, timeout, ct).ConfigureAwait(false);
    }


    /// <summary>
    /// API 34+ path. RegisterServiceInfoCallback streams updates, supports concurrent resolves
    /// and can be cleanly unregistered, so none of the legacy serialisation applies.
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("android34.0")]
    async Task<NsdServiceInfo?> ResolveViaCallback(NsdServiceInfo info, TimeSpan timeout, CancellationToken ct)
    {
        var completion = new TaskCompletionSource<NsdServiceInfo?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new NsdServiceInfoCallback(
            updated => completion.TrySetResult(updated),
            error =>
            {
                logger.LogDebug("NsdManager could not resolve {Name} - error {Error}", info.ServiceName, error);
                completion.TrySetResult(null);
            }
        );

        var nsd = this.Native;
        nsd.RegisterServiceInfoCallback(info, DirectExecutor.Instance, callback);
        try
        {
            return await completion.Task.WaitAsync(timeout, ct).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            logger.LogDebug("Resolving {Name} timed out after {Timeout}", info.ServiceName, timeout);
            return null;
        }
        finally
        {
            try
            {
                nsd.UnregisterServiceInfoCallback(callback);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "NsdManager rejected UnregisterServiceInfoCallback");
            }
        }
    }


    /// <summary>
    /// Pre-API 34 path. Callers queue behind a live resolve with their own timeout; while the slot is
    /// held by one that timed out (and may never report back), or NsdManager says another resolve is
    /// already active, they resolve through the managed mDNS stack instead.
    /// </summary>
    async Task<MdnsService?> ResolveViaListener(NsdServiceInfo info, ServiceTypeInfo serviceType, TimeSpan timeout, CancellationToken ct)
    {
        var elapsed = Stopwatch.StartNew();
        var (outcome, ticket) = await this.legacySlot.AcquireAsync(timeout, ct).ConfigureAwait(false);

        if (outcome == LegacyResolveSlotOutcome.TimedOut)
        {
            logger.LogDebug("Resolving {Name} timed out after {Timeout} waiting for NsdManager's resolve slot", info.ServiceName, timeout);
            return null;
        }

        if (outcome == LegacyResolveSlotOutcome.Orphaned)
            return await this.ResolveAroundOrphan(info, serviceType, timeout - elapsed.Elapsed, ct).ConfigureAwait(false);

        var (resolved, alreadyActive) = await this
            .ResolveWithTicket(ticket!, info, timeout, ct)
            .ConfigureAwait(false);

        if (alreadyActive)
            return await this.ResolveViaManaged(info, serviceType, timeout - elapsed.Elapsed, ct).ConfigureAwait(false);

        return resolved == null ? null : Convert(resolved, serviceType);
    }


    async Task<(NsdServiceInfo? Resolved, bool AlreadyActive)> ResolveWithTicket(
        LegacyResolveSlot.Ticket ticket,
        NsdServiceInfo info,
        TimeSpan timeout,
        CancellationToken ct
    )
    {
        var completion = new TaskCompletionSource<(NsdServiceInfo?, bool)>(TaskCreationOptions.RunContinuationsAsynchronously);

        // deliberately not disposed: NsdManager keeps the listener registered after a timeout, and
        // calling back into a disposed Java peer crashes the process. That late callback is also
        // the only thing that frees the slot, so the ticket is completed before the caller resumes.
        var listener = new NsdResolveListener(
            resolved =>
            {
                ticket.Complete();
                completion.TrySetResult((resolved, false));
            },
            error =>
            {
                ticket.Complete();
                logger.LogDebug("NsdManager could not resolve {Name} - {Error}", info.ServiceName, error);
                completion.TrySetResult((null, error == NsdFailure.AlreadyActive));
            }
        );

        try
        {
#pragma warning disable CA1422 // superseded by RegisterServiceInfoCallback in API 34, handled above
            this.Native.ResolveService(info, listener);
#pragma warning restore CA1422
        }
        catch (Exception ex)
        {
            // never reached NsdService, so there is nothing outstanding to wait for
            ticket.Complete();
            logger.LogWarning(ex, "NsdManager rejected the resolve of {Name}", info.ServiceName);
            return (null, false);
        }

        try
        {
            return await completion.Task.WaitAsync(timeout, ct).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            ticket.Orphan();
            logger.LogDebug("Resolving {Name} timed out after {Timeout} - NsdManager's resolve slot stays taken until it reports back", info.ServiceName, timeout);
            return (null, false);
        }
        catch (OperationCanceledException)
        {
            ticket.Orphan();
            throw;
        }
    }


    /// <summary>
    /// The slot is held by a resolve that timed out. Resolve through the managed stack; if that cannot
    /// run here, wait out the rest of the window in case the orphan reports back.
    /// </summary>
    async Task<MdnsService?> ResolveAroundOrphan(NsdServiceInfo info, ServiceTypeInfo serviceType, TimeSpan timeout, CancellationToken ct)
    {
        var elapsed = Stopwatch.StartNew();
        try
        {
            return await this.ResolveViaManagedCore(info, serviceType, timeout, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "The managed mDNS fallback could not resolve {Name} - queuing behind NsdManager's stuck resolve instead", info.ServiceName);
        }

        var remaining = timeout - elapsed.Elapsed;
        var (outcome, ticket) = await this.legacySlot
            .AcquireAsync(remaining, ct, waitBehindOrphan: true)
            .ConfigureAwait(false);

        if (outcome != LegacyResolveSlotOutcome.Acquired)
        {
            logger.LogDebug("Resolving {Name} timed out after {Timeout} - NsdManager's resolve slot never came free", info.ServiceName, timeout);
            return null;
        }

        var (resolved, _) = await this
            .ResolveWithTicket(ticket!, info, timeout - elapsed.Elapsed, ct)
            .ConfigureAwait(false);

        return resolved == null ? null : Convert(resolved, serviceType);
    }


    async Task<MdnsService?> ResolveViaManaged(NsdServiceInfo info, ServiceTypeInfo serviceType, TimeSpan timeout, CancellationToken ct)
    {
        try
        {
            return await this.ResolveViaManagedCore(info, serviceType, timeout, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "The managed mDNS fallback could not resolve {Name}", info.ServiceName);
            return null;
        }
    }


    Task<MdnsService?> ResolveViaManagedCore(NsdServiceInfo info, ServiceTypeInfo serviceType, TimeSpan timeout, CancellationToken ct)
    {
        if (timeout <= TimeSpan.Zero)
            return Task.FromResult<MdnsService?>(null);

        logger.LogDebug("NsdManager's resolve slot is unavailable - resolving {Name} through the managed mDNS stack", info.ServiceName);

        // the managed manager's socket set acquires the WifiManager.MulticastLock when the resolve's
        // browse opens its sockets and releases it when they close
        return this.managed.Value.Resolve(info.ServiceName ?? String.Empty, serviceType.QualifiedName, timeout, ct);
    }


    public async Task<IMdnsPublication> Publish(MdnsServiceRegistration registration, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ServiceTypeParser.ValidateInstanceName(registration.InstanceName);

        if (registration.Port is <= 0 or > 65535)
            throw new MdnsException($"The port must be between 1 and 65535 but was {registration.Port}.");

        var serviceType = ServiceTypeParser.Parse(registration.ServiceType, registration.Domain);

        var info = new NsdServiceInfo
        {
            ServiceName = registration.InstanceName,
            ServiceType = serviceType.ServiceType,
            Port = registration.Port
        };

        if (registration.TxtRecords != null)
        {
            foreach (var pair in registration.TxtRecords)
                info.SetAttribute(pair.Key, pair.Value);
        }

        var completion = new TaskCompletionSource<NsdServiceInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = new NsdRegistrationListener(
            registered => completion.TrySetResult(registered),
            error => completion.TrySetException(new MdnsException(
                $"NsdManager refused to publish '{registration.InstanceName}' - {error}"
            ))
        );

        var nsd = this.Native;
        nsd.RegisterService(info, NsdProtocol.DnsSd, listener);

        try
        {
            var result = await completion.Task.WaitAsync(ct).ConfigureAwait(false);

            // NsdManager renames on conflict, so the live name is whatever came back
            return new AndroidPublication(
                nsd,
                listener,
                result.ServiceName ?? registration.InstanceName,
                serviceType,
                registration.Port,
                logger
            );
        }
        catch
        {
            TryUnregister(nsd, listener, logger);
            throw;
        }
    }


    internal static void TryUnregister(NsdManager nsd, NsdRegistrationListener listener, ILogger logger)
    {
        try
        {
            nsd.UnregisterService(listener);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "NsdManager rejected UnregisterService");
        }
    }


    static MdnsService Convert(NsdServiceInfo info, ServiceTypeInfo serviceType) => new()
    {
        InstanceName = info.ServiceName ?? String.Empty,
        ServiceType = serviceType.ServiceType,
        Domain = serviceType.Domain,
        HostName = GetHostName(info),
        Port = info.Port,
        Addresses = GetAddresses(info),
        TxtRecords = GetTxtRecords(info)
    };


    static string? GetHostName(NsdServiceInfo info)
    {
#pragma warning disable CA1422 // NsdServiceInfo.Host is deprecated in API 34 but is the only host name source below it
        var host = OperatingSystem.IsAndroidVersionAtLeast(34)
            ? info.HostAddresses?.FirstOrDefault()
            : info.Host;
#pragma warning restore CA1422

        return host?.HostName;
    }


    static IReadOnlyList<IPAddress> GetAddresses(NsdServiceInfo info)
    {
        var sources = OperatingSystem.IsAndroidVersionAtLeast(34)
            ? info.HostAddresses?.AsEnumerable() ?? []
#pragma warning disable CA1422 // HostAddresses only exists from API 34
            : info.Host == null ? [] : [info.Host];
#pragma warning restore CA1422

        return sources
            .Select(x => x.GetAddress())
            .Where(x => x is { Length: 4 or 16 })
            .Select(x => new IPAddress(x!))
            .Distinct()
            .ToList();
    }


    static IReadOnlyDictionary<string, string> GetTxtRecords(NsdServiceInfo info)
    {
        var attributes = info.Attributes;
        if (attributes == null || attributes.Count == 0)
            return MdnsConstants.EmptyTxt;

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in attributes)
        {
            // a null value means the key was present with no "=value" part
            var value = pair.Value == null
                ? String.Empty
                : System.Text.Encoding.UTF8.GetString(pair.Value);

            result.TryAdd(pair.Key, value);
        }
        return result;
    }
}
