using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Infrastructure;


/// <summary>
/// Turns the push based <see cref="L2CapChannel.DataReceived"/> stream into a pull based byte reader.
/// </summary>
/// <remarks>
/// The platform implementations of <c>DataReceived</c> are cold - every subscription spins up
/// another socket/stream pump - so exactly one reader may exist per channel.  Instances are
/// therefore cached against the channel (see <see cref="Get"/>) and shared by every transfer
/// running over it, which also means bytes arriving between two transfers are not dropped.
/// </remarks>
sealed class L2CapChannelReader : IDisposable
{
    static readonly ConditionalWeakTable<L2CapChannel, L2CapChannelReader> readers = new();

    /// <summary>
    /// Gets (creating on first use) the single reader attached to the supplied channel.
    /// </summary>
    public static L2CapChannelReader Get(L2CapChannel channel)
        => readers.GetValue(channel, c => new L2CapChannelReader(c.DataReceived));


    /// <summary>
    /// Drops the reader attached to the channel (if any) and unsubscribes its platform pump.
    /// </summary>
    public static void Release(L2CapChannel channel)
    {
        if (readers.TryGetValue(channel, out var reader))
        {
            readers.Remove(channel);
            reader.Dispose();
        }
    }


    readonly Channel<byte[]> queue = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = true
    });
    readonly IDisposable sub;
    byte[]? current;
    int currentOffset;


    L2CapChannelReader(IObservable<byte[]> dataReceived)
        => this.sub = dataReceived.Subscribe(
            data =>
            {
                if (data.Length > 0)
                    this.queue.Writer.TryWrite(data);
            },
            ex => this.queue.Writer.TryComplete(ex),
            () => this.queue.Writer.TryComplete()
        );


    /// <summary>
    /// Set when a transfer aborts part way through the body.  The peer's remaining bytes are still
    /// in flight at that point, so anything read afterwards would be garbage.
    /// </summary>
    public bool IsFaulted { get; private set; }

    public void Fault() => this.IsFaulted = true;

    public void AssertUsable()
    {
        if (this.IsFaulted)
        {
            throw new L2CapTransferException(
                L2CapTransferError.ProtocolError,
                "A previous transfer on this channel was aborted mid-body, leaving unread bytes in flight. Close the channel and open a new one."
            );
        }
    }


    /// <summary>
    /// Reads up to <paramref name="count"/> bytes. Returns 0 when the channel has closed.
    /// </summary>
    public async ValueTask<int> Read(byte[] buffer, int offset, int count, TimeSpan? idleTimeout, CancellationToken cancellationToken)
    {
        if (count == 0)
            return 0;

        if (this.current == null)
        {
            var next = await this.Dequeue(idleTimeout, cancellationToken).ConfigureAwait(false);
            if (next == null)
                return 0;

            this.current = next;
            this.currentOffset = 0;
        }

        var available = this.current.Length - this.currentOffset;
        var take = Math.Min(available, count);
        Buffer.BlockCopy(this.current, this.currentOffset, buffer, offset, take);
        this.currentOffset += take;

        if (this.currentOffset >= this.current.Length)
        {
            this.current = null;
            this.currentOffset = 0;
        }
        return take;
    }


    /// <summary>
    /// Reads exactly <paramref name="count"/> bytes or throws.
    /// </summary>
    public async Task ReadExactly(byte[] buffer, int offset, int count, TimeSpan? idleTimeout, CancellationToken cancellationToken)
    {
        var read = 0;
        while (read < count)
        {
            var chunk = await this.Read(buffer, offset + read, count - read, idleTimeout, cancellationToken).ConfigureAwait(false);
            if (chunk == 0)
                throw new EndOfStreamException($"L2CAP channel closed with {count - read} of {count} bytes outstanding");

            read += chunk;
        }
    }


    /// <summary>
    /// Reads the next control frame, or null when the channel closed cleanly before one started.
    /// </summary>
    public async Task<L2CapFrame?> ReadFrame(TimeSpan? idleTimeout, CancellationToken cancellationToken)
    {
        var header = new byte[L2CapProtocol.HeaderSize];

        // peek the opcode separately so a clean close between transfers is not an error
        var first = await this.Read(header, 0, 1, idleTimeout, cancellationToken).ConfigureAwait(false);
        if (first == 0)
            return null;

        await this.ReadExactly(header, 1, L2CapProtocol.HeaderSize - 1, idleTimeout, cancellationToken).ConfigureAwait(false);

        var length = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(1, 4));
        if (length < 0 || length > L2CapProtocol.MaxControlPayload)
        {
            this.Fault();
            throw new L2CapTransferException(L2CapTransferError.ProtocolError, $"Control frame length of {length} bytes is out of range - the channel is not speaking the Shiny L2CAP transfer protocol");
        }

        var payload = length == 0 ? Array.Empty<byte>() : new byte[length];
        if (length > 0)
            await this.ReadExactly(payload, 0, length, idleTimeout, cancellationToken).ConfigureAwait(false);

        return new L2CapFrame((L2CapOpCode)header[0], payload);
    }


    async ValueTask<byte[]?> Dequeue(TimeSpan? idleTimeout, CancellationToken cancellationToken)
    {
        if (idleTimeout == null || idleTimeout.Value <= TimeSpan.Zero || idleTimeout.Value == Timeout.InfiniteTimeSpan)
        {
            return await this.TryDequeue(cancellationToken).ConfigureAwait(false);
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(idleTimeout.Value);
        try
        {
            return await this.TryDequeue(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            this.Fault();
            throw new TimeoutException($"No L2CAP data received within {idleTimeout.Value}");
        }
    }


    async ValueTask<byte[]?> TryDequeue(CancellationToken cancellationToken)
    {
        try
        {
            return await this.queue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException ex)
        {
            // completed with an error from the platform pump
            if (ex.InnerException != null)
                throw ex.InnerException;

            return null;
        }
    }


    public void Dispose()
    {
        this.sub.Dispose();
        this.queue.Writer.TryComplete();
    }
}
