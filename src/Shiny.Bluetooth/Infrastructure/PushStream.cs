using System.Threading.Channels;

namespace Shiny.Bluetooth.Infrastructure;


/// <summary>
/// A <see cref="Stream"/> over a callback-driven channel: the platform pushes received bytes in with
/// <see cref="Push"/>, and writes go out through a delegate. IOBluetooth delivers RFCOMM data this way.
/// </summary>
internal sealed class PushStream : Stream
{
    readonly Channel<byte[]> incoming = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true });
    readonly Func<ReadOnlyMemory<byte>, CancellationToken, Task> write;
    readonly Action onClose;
    readonly SemaphoreSlim writeLock = new(1, 1);
    readonly SemaphoreSlim readLock = new(1, 1);
    byte[]? current;
    int currentOffset;
    int closed;


    public PushStream(Func<ReadOnlyMemory<byte>, CancellationToken, Task> write, Action onClose)
    {
        this.write = write;
        this.onClose = onClose;
    }


    /// <summary>Queues bytes received from the remote side. Ignored once the stream has ended.</summary>
    public void Push(ReadOnlySpan<byte> data)
    {
        if (data.Length > 0)
            this.incoming.Writer.TryWrite(data.ToArray());
    }


    /// <summary>Ends the read side: once the queued bytes are read, reads return 0 - or throw <paramref name="error"/>.</summary>
    public void Complete(Exception? error = null) => this.incoming.Writer.TryComplete(error);


    public override bool CanRead => true;
    public override bool CanWrite => true;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }


    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        if (buffer.Length == 0)
            return 0;

        await this.readLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (this.current == null)
            {
                var available = await this.WaitForData(ct).ConfigureAwait(false);
                if (!available)
                    return 0;
            }

            var count = Math.Min(buffer.Length, this.current!.Length - this.currentOffset);
            this.current.AsMemory(this.currentOffset, count).CopyTo(buffer);
            this.currentOffset += count;
            if (this.currentOffset == this.current.Length)
            {
                this.current = null;
                this.currentOffset = 0;
            }
            return count;
        }
        finally
        {
            this.readLock.Release();
        }
    }


    async Task<bool> WaitForData(CancellationToken ct)
    {
        try
        {
            if (!await this.incoming.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
                return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // WaitToReadAsync rethrows a completion error as itself, not as a ChannelClosedException
            var cause = ex is ChannelClosedException { InnerException: { } inner } ? inner : ex;
            throw new IOException(cause.Message, cause);
        }

        if (!this.incoming.Reader.TryRead(out this.current))
            return false;

        this.currentOffset = 0;
        return true;
    }


    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        => this.ReadAsync(buffer.AsMemory(offset, count), ct).AsTask();

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("This stream is asynchronous only - use ReadAsync");


    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(this.closed == 1, this);
        if (buffer.Length == 0)
            return;

        await this.writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await this.write(buffer, ct).ConfigureAwait(false);
        }
        finally
        {
            this.writeLock.Release();
        }
    }


    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        => this.WriteAsync(buffer.AsMemory(offset, count), ct).AsTask();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("This stream is asynchronous only - use WriteAsync");


    public override void Flush() { }
    public override Task FlushAsync(CancellationToken ct) => Task.CompletedTask;
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();


    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref this.closed, 1) == 0)
        {
            this.incoming.Writer.TryComplete();
            this.onClose();
        }
        base.Dispose(disposing);
    }
}
