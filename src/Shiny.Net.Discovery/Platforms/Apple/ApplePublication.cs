using CoreFoundation;
using Foundation;
using Shiny.Net.Discovery.Internals;

namespace Shiny.Net.Discovery;


/// <summary>
/// Wraps the published NSNetService. Bonjour owns the probe/announce/goodbye handshake, so this
/// only has to keep the service alive and stop it on dispose.
/// </summary>
sealed class ApplePublication(NSNetService service, ServiceTypeInfo serviceType, int port) : IMdnsPublication
{
    int disposed;

    /// <summary>
    /// Read from the service rather than cached - Bonjour rewrites Name when it has to resolve
    /// a conflict, so the requested name and the live name can differ.
    /// </summary>
    public string InstanceName => service.Name;

    public string ServiceType { get; } = serviceType.ServiceType;

    public string Domain { get; } = serviceType.Domain;

    public int Port { get; } = port;


    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref this.disposed, 1) == 1)
            return ValueTask.CompletedTask;

        var completion = new TaskCompletionSource();
        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            try
            {
                service.Stop();
                service.Dispose();
            }
            finally
            {
                completion.TrySetResult();
            }
        });
        return new ValueTask(completion.Task);
    }
}
