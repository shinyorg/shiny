using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Android.Content;
using Android.Net.Nsd;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Internals;
using Application = Android.App.Application;

namespace Shiny.Net.Discovery;


/// <summary>
/// Browses and publishes through NsdManager, the system DNS-SD service. Going through the
/// platform means no CHANGE_WIFI_MULTICAST_STATE permission and no WifiManager.MulticastLock -
/// the OS keeps the multicast path alive for us.
/// </summary>
public class AndroidMdnsManager(ILogger<AndroidMdnsManager> logger) : IMdnsManager
{
    static readonly TimeSpan defaultResolveTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Pre-API 34 NsdManager only tolerates one resolve in flight; a second concurrent call comes
    /// back as FailureAlreadyActive. The legacy path queues through here.
    /// </summary>
    readonly SemaphoreSlim resolveLock = new(1, 1);

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

        var listener = new NsdDiscoveryListener(
            found => _ = this.OnServiceFound(found, serviceType, config, channel, ct),
            lost => channel.Writer.TryWrite(new MdnsBrowseResult(
                MdnsBrowseStatus.Lost,
                new MdnsService
                {
                    InstanceName = lost.ServiceName ?? String.Empty,
                    ServiceType = serviceType.ServiceType,
                    Domain = serviceType.Domain
                }
            )),
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
        CancellationToken ct
    )
    {
        try
        {
            var final = config.ResolveServices
                ? await this.ResolveNative(info, config.ResolveTimeout, ct).ConfigureAwait(false) ?? info
                : info;

            channel.Writer.TryWrite(new MdnsBrowseResult(MdnsBrowseStatus.Found, Convert(final, serviceType)));
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

        var resolved = await this
            .ResolveNative(request, timeout ?? defaultResolveTimeout, ct)
            .ConfigureAwait(false);

        return resolved == null ? null : Convert(resolved, info);
    }


    Task<NsdServiceInfo?> ResolveNative(NsdServiceInfo info, TimeSpan timeout, CancellationToken ct)
        => OperatingSystem.IsAndroidVersionAtLeast(34)
            ? this.ResolveViaCallback(info, timeout, ct)
            : this.ResolveViaListener(info, timeout, ct);


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
    /// Pre-API 34 path, serialised because NsdManager only tolerates one resolve at a time.
    /// </summary>
    async Task<NsdServiceInfo?> ResolveViaListener(NsdServiceInfo info, TimeSpan timeout, CancellationToken ct)
    {
        await this.resolveLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var completion = new TaskCompletionSource<NsdServiceInfo?>(TaskCreationOptions.RunContinuationsAsynchronously);

            // deliberately not disposed: NsdManager keeps the listener registered even after a
            // timeout, and calling back into a disposed Java peer crashes the process
            var listener = new NsdResolveListener(
                resolved => completion.TrySetResult(resolved),
                error =>
                {
                    logger.LogDebug("NsdManager could not resolve {Name} - {Error}", info.ServiceName, error);
                    completion.TrySetResult(null);
                }
            );

#pragma warning disable CA1422 // superseded by RegisterServiceInfoCallback in API 34, handled above
            this.Native.ResolveService(info, listener);
#pragma warning restore CA1422
            return await completion.Task.WaitAsync(timeout, ct).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            logger.LogDebug("Resolving {Name} timed out after {Timeout}", info.ServiceName, timeout);
            return null;
        }
        finally
        {
            this.resolveLock.Release();
        }
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
