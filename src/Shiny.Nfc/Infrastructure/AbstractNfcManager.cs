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
                new BoundedChannelOptions(1)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false
                },
                this.OnTagDropped
            );
            var sink = new NfcScanSink(channel.Writer);

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
        (tag as IDisposable)?.Dispose();
    }
}


/// <summary>
/// Where a backend reports what its scan session sees. Safe to call from any thread, and after the
/// consumer has stopped listening.
/// </summary>
sealed class NfcScanSink(ChannelWriter<INfcTag> writer)
{
    /// <summary>A tag came into range.</summary>
    public void Report(INfcTag tag)
    {
        if (!writer.TryWrite(tag))
            (tag as IDisposable)?.Dispose();
    }

    /// <summary>The session ended without error - the user dismissed the scan UI, for instance.</summary>
    public void Complete() => writer.TryComplete();

    /// <summary>The session ended with an error, which the consumer's loop rethrows.</summary>
    public void Fail(Exception exception) => writer.TryComplete(exception);
}
