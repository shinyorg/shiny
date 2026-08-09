using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


/// <summary>
/// File-transfer helpers over an open <see cref="L2CapChannel"/>.
/// </summary>
public static class L2CapChannelExtensions
{
    /// <summary>
    /// Streams the contents of <paramref name="filePath"/> over the channel, invoking
    /// <paramref name="onProgress"/> with throughput / percent-complete / ETA metrics
    /// roughly every two seconds and once more on completion.
    /// </summary>
    /// <param name="channel">The open L2CAP channel.</param>
    /// <param name="filePath">Path of the file to send. Its length is used as the total transfer size.</param>
    /// <param name="bufferSize">Bytes per write. Defaults to 4096.</param>
    /// <param name="onProgress">Optional progress callback. Invoked on the producing async context.</param>
    /// <param name="cancellationToken">Token used to cancel the transfer.</param>
    public static async Task SendFile(
        this L2CapChannel channel,
        string filePath,
        int bufferSize = 4096,
        Action<TransferProgress>? onProgress = null,
        CancellationToken cancellationToken = default
    )
    {
        var file = new FileInfo(filePath);
        using var stream = file.OpenRead();
        await channel
            .SendFile(stream, file.Length, bufferSize, onProgress, cancellationToken)
            .ConfigureAwait(false);
    }


    /// <summary>
    /// Streams the contents of <paramref name="source"/> over the channel, invoking
    /// <paramref name="onProgress"/> with throughput / percent-complete / ETA metrics
    /// roughly every two seconds and once more on completion.
    /// </summary>
    /// <param name="channel">The open L2CAP channel.</param>
    /// <param name="source">The stream to read from. Read up to EOF; not disposed by this method.</param>
    /// <param name="totalBytes">Total bytes the stream will yield, or null when unknown. Required for percent-complete and ETA.</param>
    /// <param name="bufferSize">Bytes per write. Defaults to 4096.</param>
    /// <param name="onProgress">Optional progress callback. Invoked on the producing async context.</param>
    /// <param name="cancellationToken">Token used to cancel the transfer.</param>
    public static async Task SendFile(
        this L2CapChannel channel,
        Stream source,
        long? totalBytes = null,
        int bufferSize = 4096,
        Action<TransferProgress>? onProgress = null,
        CancellationToken cancellationToken = default
    )
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (bufferSize < 1) throw new ArgumentOutOfRangeException(nameof(bufferSize), "bufferSize must be at least 1");

        var buffer = new byte[bufferSize];
        var totalBytesXfer = 0L;
        var totalSince = 0L;
        var window = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var read = await source
                .ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
                break;

            // Hand Write its own array every time. It only promises the bytes are *queued*, so a
            // reused buffer could be overwritten before the channel gets around to them - and a
            // short read would otherwise trail stale bytes from the previous chunk.
            await channel
                .Write(buffer.AsSpan(0, read).ToArray())
                .ToTask(cancellationToken)
                .ConfigureAwait(false);

            totalBytesXfer += read;
            totalSince += read;

            if (window.Elapsed.TotalSeconds > 2 && onProgress != null)
            {
                var bytesPerSecond = (long)(totalSince / window.Elapsed.TotalSeconds);
                onProgress(new TransferProgress(bytesPerSecond, totalBytes, totalBytesXfer));
                totalSince = 0;
                window.Restart();
            }
        }

        onProgress?.Invoke(new TransferProgress(0, totalBytes ?? totalBytesXfer, totalBytesXfer));
    }
}
