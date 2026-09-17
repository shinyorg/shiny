using System;
using System.Buffers;
using System.IO;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Shiny.BluetoothLE.Infrastructure;

namespace Shiny.BluetoothLE;


/// <summary>
/// An open <see cref="L2CapChannel"/> as a <see cref="Stream"/>, so it can be handed to anything that reads and
/// writes streams - a JSON serializer, <see cref="Stream.CopyToAsync(Stream)"/>, a hash, a decoder.
/// </summary>
/// <remarks>
/// <para>Reads come from the same buffered reader the L2CAP file transfer helpers use, so a stream and a file
/// transfer can take turns on one channel without losing bytes between them. The reader subscribes to
/// <see cref="L2CapChannel.DataReceived"/> when the stream is created, so create it as soon as the channel
/// opens - data a peer sends before anything is subscribed is lost.</para>
/// <para>Asynchronous only. The synchronous <see cref="Read(byte[], int, int)"/> and
/// <see cref="Write(byte[], int, int)"/> throw, because blocking a thread on a radio link is never what you want.</para>
/// </remarks>
public sealed class L2CapChannelStream : Stream
{
    readonly L2CapChannel channel;
    readonly L2CapChannelReader reader;
    readonly bool leaveOpen;
    int maxWriteSize;
    int disposed;


    /// <param name="channel">The open channel.</param>
    /// <param name="maxWriteSize">The largest single write handed to the channel. Larger writes are split.</param>
    /// <param name="leaveOpen">When false (the default) disposing the stream closes the channel.</param>
    public L2CapChannelStream(L2CapChannel channel, int maxWriteSize = DefaultMaxWriteSize, bool leaveOpen = false)
    {
        this.channel = channel ?? throw new ArgumentNullException(nameof(channel));
        this.MaxWriteSize = maxWriteSize;
        this.leaveOpen = leaveOpen;
        this.reader = L2CapChannelReader.Get(channel);
    }


    /// <summary>The write size used when none is given.</summary>
    public const int DefaultMaxWriteSize = 4096;


    /// <summary>Gets the channel this stream reads and writes.</summary>
    public L2CapChannel Channel => this.channel;

    /// <summary>
    /// Gets or sets the largest single write handed to the channel. Adjustable after creation, because what suits
    /// a channel is often not known until it has said what it is for.
    /// </summary>
    public int MaxWriteSize
    {
        get => this.maxWriteSize;
        set
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), "MaxWriteSize must be at least 1");

            this.maxWriteSize = value;
        }
    }

    /// <summary>Gets the bytes read from the channel so far.</summary>
    public long BytesRead { get; private set; }

    /// <summary>Gets the bytes written to the channel so far.</summary>
    public long BytesWritten { get; private set; }


    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.Length == 0)
            return 0;

        int read;
        if (MemoryMarshal.TryGetArray<byte>(buffer, out var segment))
        {
            read = await this.reader.Read(segment.Array!, segment.Offset, segment.Count, null, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var rented = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                read = await this.reader.Read(rented, 0, buffer.Length, null, cancellationToken).ConfigureAwait(false);
                rented.AsSpan(0, read).CopyTo(buffer.Span);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        this.BytesRead += read;
        return read;
    }


    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => this.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();


    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var count = Math.Min(this.maxWriteSize, buffer.Length - offset);

            // a fresh array every time: Write only promises the bytes are queued, so a caller's reused buffer
            // could be overwritten before the channel gets to it
            await this.channel
                .Write(buffer.Slice(offset, count).ToArray())
                .ToTask(cancellationToken)
                .ConfigureAwait(false);

            offset += count;
            this.BytesWritten += count;
        }
    }


    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => this.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();


    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Flush() { }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("Read an L2CAP channel asynchronously");

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("Write an L2CAP channel asynchronously");


    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref this.disposed, 1) == 0 && !this.leaveOpen)
            this.channel.Dispose();

        base.Dispose(disposing);
    }
}


/// <summary>
/// Stream conveniences for <see cref="L2CapChannel"/>.
/// </summary>
public static class L2CapChannelStreamExtensions
{
    /// <summary>
    /// Wraps the channel as a <see cref="Stream"/>. See <see cref="L2CapChannelStream"/>.
    /// </summary>
    /// <param name="channel">The open channel.</param>
    /// <param name="maxWriteSize">The largest single write handed to the channel.</param>
    /// <param name="leaveOpen">When false (the default) disposing the stream closes the channel.</param>
    public static L2CapChannelStream AsStream(this L2CapChannel channel, int maxWriteSize = L2CapChannelStream.DefaultMaxWriteSize, bool leaveOpen = false)
        => new(channel, maxWriteSize, leaveOpen);
}
