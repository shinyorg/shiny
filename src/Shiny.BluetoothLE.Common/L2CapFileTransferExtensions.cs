using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Shiny.BluetoothLE.Infrastructure;

namespace Shiny.BluetoothLE;


/// <summary>
/// File upload/download over an open <see cref="L2CapChannel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Both peers of an L2CAP channel get the same <see cref="L2CapChannel"/> type, so these methods work
/// identically from the central (client) and the peripheral (hosting) side.  One side drives with
/// <see cref="UploadFile(L2CapChannel, string, string?, Action{TransferProgress}?, L2CapTransferOptions?, CancellationToken)"/>
/// or <see cref="DownloadFile(L2CapChannel, string, string, Action{TransferProgress}?, L2CapTransferOptions?, CancellationToken)"/>;
/// the other serves with <see cref="ReadFileRequest"/> and the accept/reject methods on
/// <see cref="L2CapFileRequest"/>.
/// </para>
/// <para>
/// Transfers are sequential on a given channel - run one at a time, and do not subscribe to
/// <see cref="L2CapChannel.DataReceived"/> yourself while a transfer is in flight.
/// </para>
/// </remarks>
public static class L2CapFileTransferExtensions
{
    /// <summary>
    /// Sends a local file to the remote peer, which must be serving with <see cref="ReadFileRequest"/>.
    /// </summary>
    /// <param name="channel">The open L2CAP channel.</param>
    /// <param name="localFilePath">Path of the file to send.</param>
    /// <param name="remoteFileName">Name to store it under on the peer. Defaults to the local file name.</param>
    /// <param name="onProgress">Optional progress callback - percent complete, throughput, and ETA.</param>
    /// <param name="options">Optional buffer/interval/timeout tuning.</param>
    /// <param name="cancellationToken">Token used to cancel the transfer.</param>
    /// <returns>The transfer outcome.</returns>
    /// <exception cref="L2CapTransferException">The peer refused the file or the peers fell out of sync.</exception>
    public static async Task<L2CapTransferResult> UploadFile(
        this L2CapChannel channel,
        string localFilePath,
        string? remoteFileName = null,
        Action<TransferProgress>? onProgress = null,
        L2CapTransferOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (localFilePath == null) throw new ArgumentNullException(nameof(localFilePath));

        var file = new FileInfo(localFilePath);
        if (!file.Exists)
            throw new FileNotFoundException("File to upload was not found", localFilePath);

        using var stream = file.OpenRead();
        return await channel
            .UploadFile(stream, remoteFileName ?? file.Name, file.Length, onProgress, options, cancellationToken)
            .ConfigureAwait(false);
    }


    /// <summary>
    /// Sends a stream to the remote peer, which must be serving with <see cref="ReadFileRequest"/>.
    /// </summary>
    /// <param name="channel">The open L2CAP channel.</param>
    /// <param name="source">The stream to send. Read until <paramref name="length"/> bytes have been sent; not disposed by this method.</param>
    /// <param name="remoteFileName">Name to store it under on the peer.</param>
    /// <param name="length">Exact number of bytes that will be sent. Required - the peer preallocates against it and uses it for percent complete.</param>
    /// <param name="onProgress">Optional progress callback - percent complete, throughput, and ETA.</param>
    /// <param name="options">Optional buffer/interval/timeout tuning.</param>
    /// <param name="cancellationToken">Token used to cancel the transfer.</param>
    /// <returns>The transfer outcome.</returns>
    /// <exception cref="L2CapTransferException">The peer refused the file or the peers fell out of sync.</exception>
    public static async Task<L2CapTransferResult> UploadFile(
        this L2CapChannel channel,
        Stream source,
        string remoteFileName,
        long length,
        Action<TransferProgress>? onProgress = null,
        L2CapTransferOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative");

        options ??= L2CapTransferOptions.Default;
        options.Assert();

        var reader = L2CapChannelReader.Get(channel);
        reader.AssertUsable();

        await channel.WriteFrame(L2CapProtocol.Put(remoteFileName, length), cancellationToken).ConfigureAwait(false);

        var accept = await reader.Expect(L2CapOpCode.Accept, options, cancellationToken).ConfigureAwait(false);
        var (agreedName, _) = accept.ReadNameAndSize();

        var reporter = await channel
            .SendBody(reader, source, length, options, onProgress, cancellationToken)
            .ConfigureAwait(false);

        var ack = await reader.Expect(L2CapOpCode.Ack, options, cancellationToken).ConfigureAwait(false);
        var received = ack.ReadAck();
        if (received != length)
        {
            reader.Fault();
            throw new L2CapTransferException(L2CapTransferError.ProtocolError, $"Peer acknowledged {received} bytes but {length} were sent");
        }

        return new L2CapTransferResult(L2CapTransferType.Upload, agreedName, reporter.BytesTransferred, reporter.Elapsed);
    }


    /// <summary>
    /// Requests a file from the remote peer and writes it to <paramref name="localFilePath"/>.
    /// </summary>
    /// <param name="channel">The open L2CAP channel.</param>
    /// <param name="remoteFileName">Name of the file to fetch, as the peer knows it.</param>
    /// <param name="localFilePath">Path to write to. Created (or overwritten) and truncated to the received length.</param>
    /// <param name="onProgress">Optional progress callback - percent complete, throughput, and ETA.</param>
    /// <param name="options">Optional buffer/interval/timeout tuning.</param>
    /// <param name="cancellationToken">Token used to cancel the transfer.</param>
    /// <returns>The transfer outcome.</returns>
    /// <exception cref="L2CapTransferException">The peer does not have the file, refused it, or the peers fell out of sync.</exception>
    public static async Task<L2CapTransferResult> DownloadFile(
        this L2CapChannel channel,
        string remoteFileName,
        string localFilePath,
        Action<TransferProgress>? onProgress = null,
        L2CapTransferOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (localFilePath == null) throw new ArgumentNullException(nameof(localFilePath));

        var dir = Path.GetDirectoryName(Path.GetFullPath(localFilePath));
        if (!String.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        try
        {
            using var stream = File.Create(localFilePath);
            return await channel
                .DownloadFile(remoteFileName, stream, onProgress, options, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            // don't leave a truncated/empty file behind for a transfer that never landed
            try { File.Delete(localFilePath); } catch { /* best effort */ }
            throw;
        }
    }


    /// <summary>
    /// Requests a file from the remote peer and writes it to <paramref name="destination"/>.
    /// </summary>
    /// <param name="channel">The open L2CAP channel.</param>
    /// <param name="remoteFileName">Name of the file to fetch, as the peer knows it.</param>
    /// <param name="destination">The stream to write to. Not disposed by this method.</param>
    /// <param name="onProgress">Optional progress callback - percent complete, throughput, and ETA.</param>
    /// <param name="options">Optional buffer/interval/timeout tuning.</param>
    /// <param name="cancellationToken">Token used to cancel the transfer.</param>
    /// <returns>The transfer outcome.</returns>
    /// <exception cref="L2CapTransferException">The peer does not have the file, refused it, or the peers fell out of sync.</exception>
    public static async Task<L2CapTransferResult> DownloadFile(
        this L2CapChannel channel,
        string remoteFileName,
        Stream destination,
        Action<TransferProgress>? onProgress = null,
        L2CapTransferOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));
        if (destination == null) throw new ArgumentNullException(nameof(destination));

        options ??= L2CapTransferOptions.Default;
        options.Assert();

        var reader = L2CapChannelReader.Get(channel);
        reader.AssertUsable();

        await channel.WriteFrame(L2CapProtocol.Get(remoteFileName), cancellationToken).ConfigureAwait(false);

        var accept = await reader.Expect(L2CapOpCode.Accept, options, cancellationToken).ConfigureAwait(false);
        var (agreedName, size) = accept.ReadNameAndSize();

        var reporter = await reader
            .ReceiveBody(destination, size, options, onProgress, cancellationToken)
            .ConfigureAwait(false);

        await channel.WriteFrame(L2CapProtocol.Ack(reporter.BytesTransferred), cancellationToken).ConfigureAwait(false);

        return new L2CapTransferResult(L2CapTransferType.Download, agreedName, reporter.BytesTransferred, reporter.Elapsed);
    }


    /// <summary>
    /// Waits for the peer's next upload or download request. Serving side of the transfer protocol.
    /// </summary>
    /// <param name="channel">The open L2CAP channel.</param>
    /// <param name="options">Optional timeout tuning. The idle timeout applies while waiting for the request.</param>
    /// <param name="cancellationToken">Token used to stop waiting.</param>
    /// <returns>The request to accept or reject, or null when the peer closed the channel.</returns>
    /// <remarks>
    /// Every returned request must be answered with one of the accept methods or
    /// <see cref="L2CapFileRequest.Reject"/> before the next one is read.
    /// </remarks>
    public static async Task<L2CapFileRequest?> ReadFileRequest(
        this L2CapChannel channel,
        L2CapTransferOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));

        options ??= L2CapTransferOptions.Default;
        options.Assert();

        var reader = L2CapChannelReader.Get(channel);
        reader.AssertUsable();

        // wait indefinitely for the request itself - the idle timeout is about a stalled transfer,
        // not about how long a peer may sit on an idle channel
        var frame = await reader.ReadFrame(null, cancellationToken).ConfigureAwait(false);
        if (frame == null)
            return null;

        switch (frame.Value.OpCode)
        {
            case L2CapOpCode.Put:
                var (putName, size) = frame.Value.ReadNameAndSize();
                return new L2CapFileRequest(channel, reader, L2CapTransferType.Upload, putName, size, options);

            case L2CapOpCode.Get:
                return new L2CapFileRequest(channel, reader, L2CapTransferType.Download, frame.Value.ReadName(), 0, options);

            case L2CapOpCode.Error:
                throw frame.Value.ToException();

            default:
                reader.Fault();
                throw new L2CapTransferException(L2CapTransferError.ProtocolError, $"Expected a Put or Get request but received {frame.Value.OpCode}");
        }
    }


    // -------- Rx variants --------

    /// <summary>
    /// Rx flavour of <see cref="UploadFile(L2CapChannel, string, string?, Action{TransferProgress}?, L2CapTransferOptions?, CancellationToken)"/>.
    /// Emits progress as the transfer runs and a final 100% event before completing.
    /// </summary>
    /// <param name="channel">The open L2CAP channel.</param>
    /// <param name="localFilePath">Path of the file to send.</param>
    /// <param name="remoteFileName">Name to store it under on the peer. Defaults to the local file name.</param>
    /// <param name="options">Optional buffer/interval/timeout tuning.</param>
    /// <remarks>Disposing the subscription cancels the transfer.</remarks>
    public static IObservable<TransferProgress> UploadFileWithProgress(
        this L2CapChannel channel,
        string localFilePath,
        string? remoteFileName = null,
        L2CapTransferOptions? options = null
    ) => Observable.Create<TransferProgress>(async (ob, ct) =>
    {
        await channel
            .UploadFile(localFilePath, remoteFileName, ob.OnNext, options, ct)
            .ConfigureAwait(false);
    });


    /// <summary>
    /// Rx flavour of <see cref="DownloadFile(L2CapChannel, string, string, Action{TransferProgress}?, L2CapTransferOptions?, CancellationToken)"/>.
    /// Emits progress as the transfer runs and a final 100% event before completing.
    /// </summary>
    /// <param name="channel">The open L2CAP channel.</param>
    /// <param name="remoteFileName">Name of the file to fetch, as the peer knows it.</param>
    /// <param name="localFilePath">Path to write to.</param>
    /// <param name="options">Optional buffer/interval/timeout tuning.</param>
    /// <remarks>Disposing the subscription cancels the transfer.</remarks>
    public static IObservable<TransferProgress> DownloadFileWithProgress(
        this L2CapChannel channel,
        string remoteFileName,
        string localFilePath,
        L2CapTransferOptions? options = null
    ) => Observable.Create<TransferProgress>(async (ob, ct) =>
    {
        await channel
            .DownloadFile(remoteFileName, localFilePath, ob.OnNext, options, ct)
            .ConfigureAwait(false);
    });


    // -------- internals shared with L2CapFileRequest --------

    internal static Task WriteFrame(this L2CapChannel channel, byte[] frame, CancellationToken cancellationToken)
        => channel.Write(frame).ToTask(cancellationToken);


    internal static async Task<L2CapFrame> Expect(
        this L2CapChannelReader reader,
        L2CapOpCode expected,
        L2CapTransferOptions options,
        CancellationToken cancellationToken
    )
    {
        var frame = await reader.ReadFrame(options.IdleTimeout, cancellationToken).ConfigureAwait(false);
        if (frame == null)
        {
            reader.Fault();
            throw new L2CapTransferException(L2CapTransferError.ProtocolError, $"Channel closed while waiting for {expected}");
        }

        if (frame.Value.OpCode == L2CapOpCode.Error)
            throw frame.Value.ToException();

        if (frame.Value.OpCode != expected)
        {
            reader.Fault();
            throw new L2CapTransferException(L2CapTransferError.ProtocolError, $"Expected {expected} but received {frame.Value.OpCode}");
        }
        return frame.Value;
    }


    internal static async Task<L2CapProgressReporter> SendBody(
        this L2CapChannel channel,
        L2CapChannelReader reader,
        Stream source,
        long length,
        L2CapTransferOptions options,
        Action<TransferProgress>? onProgress,
        CancellationToken cancellationToken
    )
    {
        var reporter = new L2CapProgressReporter(length, options.ProgressInterval, onProgress);
        var buffer = new byte[options.BufferSize];
        var remaining = length;

        try
        {
            while (remaining > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var want = (int)Math.Min(buffer.Length, remaining);
                var read = await source.ReadAsync(buffer, 0, want, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    throw new EndOfStreamException($"Source stream ended with {remaining} of {length} bytes still to send");

                // Hand Write its own array every time. It only promises the bytes are *queued*, so a
                // reused buffer could be overwritten before the channel gets around to them - and a
                // short read would otherwise trail stale bytes from the previous chunk.
                await channel
                    .Write(buffer.AsSpan(0, read).ToArray())
                    .ToTask(cancellationToken)
                    .ConfigureAwait(false);

                remaining -= read;
                reporter.Add(read);
            }
        }
        catch
        {
            // the peer is still expecting `remaining` more bytes - the channel can no longer be trusted
            reader.Fault();
            throw;
        }

        reporter.Complete();
        return reporter;
    }


    internal static async Task<L2CapProgressReporter> ReceiveBody(
        this L2CapChannelReader reader,
        Stream destination,
        long length,
        L2CapTransferOptions options,
        Action<TransferProgress>? onProgress,
        CancellationToken cancellationToken
    )
    {
        var reporter = new L2CapProgressReporter(length, options.ProgressInterval, onProgress);
        var buffer = new byte[options.BufferSize];
        var remaining = length;

        try
        {
            while (remaining > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var want = (int)Math.Min(buffer.Length, remaining);
                var read = await reader.Read(buffer, 0, want, options.IdleTimeout, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    throw new EndOfStreamException($"Channel closed with {remaining} of {length} bytes outstanding");

                await destination.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);

                remaining -= read;
                reporter.Add(read);
            }
            await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // `remaining` bytes of body are still inbound - anything read afterwards would be garbage
            reader.Fault();
            throw;
        }

        reporter.Complete();
        return reporter;
    }
}
