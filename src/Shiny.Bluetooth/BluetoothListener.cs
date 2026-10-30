using System.Threading.Channels;

namespace Shiny.Bluetooth;


/// <summary>
/// A published RFCOMM service accepting connections. Dispose to withdraw the SDP record and stop accepting.
/// </summary>
/// <remarks>
/// Connections that arrive while nobody is waiting in <see cref="Accept"/> are queued; any still
/// queued when the listener is disposed are closed.
/// </remarks>
public sealed class BluetoothListener : IAsyncDisposable
{
    readonly Channel<BluetoothConnection> pending = Channel.CreateUnbounded<BluetoothConnection>();
    readonly Func<ValueTask> stop;
    int disposed;


    internal BluetoothListener(Guid serviceUuid, string serviceName, Func<ValueTask> stop)
    {
        this.ServiceUuid = serviceUuid;
        this.ServiceName = serviceName;
        this.stop = stop;
    }


    /// <summary>The published service UUID.</summary>
    public Guid ServiceUuid { get; }

    /// <summary>The published service name.</summary>
    public string ServiceName { get; }


    /// <summary>
    /// Waits for the next incoming connection.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The listener was disposed.</exception>
    /// <exception cref="BluetoothException">The platform stopped the listener - the radio was turned off,
    /// or the Bluetooth service released the record.</exception>
    public async Task<BluetoothConnection> Accept(CancellationToken ct = default)
    {
        try
        {
            return await this.pending.Reader.ReadAsync(ct).ConfigureAwait(false);
        }
        catch (ChannelClosedException ex) when (ex.InnerException != null)
        {
            throw new BluetoothException("The listener was stopped by the platform", ex.InnerException);
        }
        catch (ChannelClosedException)
        {
            throw new ObjectDisposedException(nameof(BluetoothListener));
        }
    }


    /// <summary>Hands an accepted connection to the next <see cref="Accept"/>, or closes it if the listener is gone.</summary>
    internal void Enqueue(BluetoothConnection connection)
    {
        if (!this.pending.Writer.TryWrite(connection))
            connection.Dispose();
    }


    /// <summary>Ends the listener from the platform side; waiting and future accepts throw <paramref name="exception"/>.</summary>
    internal void Fault(Exception exception) => this.pending.Writer.TryComplete(exception);


    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref this.disposed, 1) == 1)
            return;

        this.pending.Writer.TryComplete();
        while (this.pending.Reader.TryRead(out var orphan))
            await orphan.DisposeAsync().ConfigureAwait(false);

        await this.stop().ConfigureAwait(false);
    }
}
