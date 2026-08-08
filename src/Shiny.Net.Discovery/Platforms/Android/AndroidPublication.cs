using Android.Net.Nsd;
using Microsoft.Extensions.Logging;
using Shiny.Net.Discovery.Internals;

namespace Shiny.Net.Discovery;


/// <summary>
/// Wraps an NsdManager registration. NsdManager owns the probe/announce/goodbye handshake, so
/// this only has to unregister on dispose.
/// </summary>
sealed class AndroidPublication(
    NsdManager nsd,
    NsdRegistrationListener listener,
    string instanceName,
    ServiceTypeInfo serviceType,
    int port,
    ILogger logger
) : IMdnsPublication
{
    int disposed;

    public string InstanceName { get; } = instanceName;

    public string ServiceType { get; } = serviceType.ServiceType;

    public string Domain { get; } = serviceType.Domain;

    public int Port { get; } = port;


    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref this.disposed, 1) == 0)
            AndroidMdnsManager.TryUnregister(nsd, listener, logger);

        return ValueTask.CompletedTask;
    }
}
