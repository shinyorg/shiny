using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Shiny.Nfc.Infrastructure;


/// <summary>
/// The scan session plumbing every backend shares: one scan at a time, the session started by the
/// first <c>MoveNextAsync</c> and stopped by disposing the enumerator, and a one-slot hand-off
/// between the platform's callback and the consumer's loop.
/// </summary>
/// <remarks>
/// Backends only implement <see cref="StartScan"/>: start the radio, report tags to the sink from
/// whatever thread the platform calls back on, and return something that stops the radio when
/// disposed.
/// </remarks>
abstract class AbstractNfcManager(ILogger logger) : INfcManager
{
    const int QueueCapacity = 1;
    int scanning;

    protected ILogger Logger => logger;

    public abstract NfcCapabilities Capabilities { get; }
    public abstract Task<AccessState> RequestAccess(CancellationToken ct = default);


    /// <summary>
    /// Starts the platform scan. Tags go to <paramref name="sink"/>; the returned object stops the
    /// scan when disposed, and must tolerate being disposed after the platform already ended it.
    /// </summary>
    protected abstract Task<IAsyncDisposable> StartScan(NfcScanOptions options, NfcScanSink sink, CancellationToken ct);


    public async IAsyncEnumerable<INfcTag> Scan(NfcScanOptions? options = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (Interlocked.CompareExchange(ref this.scanning, 1, 0) != 0)
            throw new InvalidOperationException("An NFC scan is already running - only one scan session can be open at a time");

        try
        {
            var channel = Channel.CreateBounded<INfcTag>(
                new BoundedChannelOptions(QueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false
                },
                this.OnTagDropped
            );
            var sink = new NfcScanSink(channel, QueueCapacity);

            this.Logger.ScanStarting();
            await using var session = await this.StartScan(options ?? new(), sink, ct).ConfigureAwait(false);

            await foreach (var tag in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
                yield return tag;
        }
        finally
        {
            this.Logger.ScanEnded();
            Volatile.Write(ref this.scanning, 0);
        }
    }


    void OnTagDropped(INfcTag tag)
    {
        this.Logger.TagDropped();
        if (tag is IDroppableNfcTag droppable)
            droppable.OnDropped();
        else
            (tag as IDisposable)?.Dispose();
    }
}


/// <summary>
/// A tag whose backend wants to know when a newer tag pushed it out of the one-slot queue before the
/// consumer read it, instead of having it simply disposed. PC/SC uses this to close the card handle
/// and offer the tag again later, since the card is still sitting on the reader.
/// </summary>
interface IDroppableNfcTag
{
    /// <summary>Called instead of <see cref="IDisposable.Dispose"/> when the queue drops the tag.</summary>
    void OnDropped();
}


/// <summary>
/// Where a backend reports what its scan session sees. Safe to call from any thread, and after the
/// consumer has stopped listening.
/// </summary>
sealed class NfcScanSink(Channel<INfcTag> channel, int capacity)
{
    // writers only ever fill the queue and the single consumer only empties it, so taking this
    // around every write makes "is there room, then write" atomic
    readonly Lock gate = new();


    /// <summary>Whether a tag can be queued right now without pushing out one the consumer has not read.</summary>
    public bool HasRoom
    {
        get
        {
            lock (this.gate)
                return channel.Reader.Count < capacity;
        }
    }


    /// <summary>A tag came into range. It replaces a queued tag the consumer has not read yet.</summary>
    public void Report(INfcTag tag)
    {
        bool written;
        lock (this.gate)
            written = channel.Writer.TryWrite(tag);

        if (!written)
            (tag as IDisposable)?.Dispose();
    }


    /// <summary>
    /// Queues a tag only if that pushes nothing else out - for offering a tag again, which must never
    /// cost another tag its place. Returns false (and leaves the tag to the caller) when the queue is
    /// full or the session has ended.
    /// </summary>
    public bool TryReportWithoutEvicting(INfcTag tag)
    {
        lock (this.gate)
            return channel.Reader.Count < capacity && channel.Writer.TryWrite(tag);
    }


    /// <summary>The session ended without error - the user dismissed the scan UI, for instance.</summary>
    public void Complete() => channel.Writer.TryComplete();

    /// <summary>The session ended with an error, which the consumer's loop rethrows.</summary>
    public void Fail(Exception exception) => channel.Writer.TryComplete(exception);
}
