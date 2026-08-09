using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shiny.BluetoothLE.Infrastructure;

namespace Shiny.BluetoothLE;


/// <summary>
/// An inbound file transfer request from the remote peer, returned by
/// <see cref="L2CapFileTransferExtensions.ReadFileRequest"/>.
/// </summary>
/// <remarks>
/// Exactly one of the accept methods or <see cref="Reject"/> must be called - the peer is blocked
/// waiting for the answer, and the channel cannot carry another request until this one is answered.
/// </remarks>
public sealed class L2CapFileRequest
{
    readonly L2CapChannel channel;
    readonly L2CapChannelReader reader;
    readonly L2CapTransferOptions options;
    int answered;


    internal L2CapFileRequest(
        L2CapChannel channel,
        L2CapChannelReader reader,
        L2CapTransferType type,
        string fileName,
        long size,
        L2CapTransferOptions options
    )
    {
        this.channel = channel;
        this.reader = reader;
        this.options = options;
        this.Type = type;
        this.FileName = fileName;
        this.Size = size;
    }


    /// <summary>
    /// Gets what the peer asked for - <see cref="L2CapTransferType.Upload"/> means it wants to send
    /// you a file, <see cref="L2CapTransferType.Download"/> means it wants one from you.
    /// </summary>
    public L2CapTransferType Type { get; }

    /// <summary>
    /// Gets the file name requested by the peer. Treat this as untrusted input - it comes off the wire.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the byte count the peer intends to send. Zero for a download request, where you supply the size.
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// Gets the remote peer identifier (peripheral UUID on Apple, MAC address elsewhere).
    /// </summary>
    public string PeerIdentifier => this.channel.Identifier;

    /// <summary>
    /// Gets the PSM the channel is running on.
    /// </summary>
    public ushort Psm => this.channel.Psm;

    /// <summary>
    /// Gets whether this request has already been accepted or rejected.
    /// </summary>
    public bool IsAnswered => this.answered == 1;


    /// <summary>
    /// Accepts an upload and writes the incoming bytes to <paramref name="localFilePath"/>.
    /// </summary>
    /// <param name="localFilePath">Path to write to. Created (or overwritten) and truncated to the received length.</param>
    /// <param name="onProgress">Optional progress callback - percent complete, throughput, and ETA.</param>
    /// <param name="cancellationToken">Token used to cancel the transfer.</param>
    /// <returns>The transfer outcome.</returns>
    public async Task<L2CapTransferResult> AcceptUpload(
        string localFilePath,
        Action<TransferProgress>? onProgress = null,
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
            return await this.AcceptUpload(stream, onProgress, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            try { File.Delete(localFilePath); } catch { /* best effort */ }
            throw;
        }
    }


    /// <summary>
    /// Accepts an upload and writes the incoming bytes to <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The stream to write to. Not disposed by this method.</param>
    /// <param name="onProgress">Optional progress callback - percent complete, throughput, and ETA.</param>
    /// <param name="cancellationToken">Token used to cancel the transfer.</param>
    /// <returns>The transfer outcome.</returns>
    public async Task<L2CapTransferResult> AcceptUpload(
        Stream destination,
        Action<TransferProgress>? onProgress = null,
        CancellationToken cancellationToken = default
    )
    {
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        this.Answer(L2CapTransferType.Upload);

        await this.channel
            .WriteFrame(L2CapProtocol.Accept(this.FileName, this.Size), cancellationToken)
            .ConfigureAwait(false);

        var reporter = await this.reader
            .ReceiveBody(destination, this.Size, this.options, onProgress, cancellationToken)
            .ConfigureAwait(false);

        await this.channel
            .WriteFrame(L2CapProtocol.Ack(reporter.BytesTransferred), cancellationToken)
            .ConfigureAwait(false);

        return new L2CapTransferResult(L2CapTransferType.Upload, this.FileName, reporter.BytesTransferred, reporter.Elapsed);
    }


    /// <summary>
    /// Accepts a download request and streams <paramref name="localFilePath"/> back to the peer.
    /// </summary>
    /// <param name="localFilePath">Path of the file to serve.</param>
    /// <param name="onProgress">Optional progress callback - percent complete, throughput, and ETA.</param>
    /// <param name="cancellationToken">Token used to cancel the transfer.</param>
    /// <returns>The transfer outcome.</returns>
    public async Task<L2CapTransferResult> AcceptDownload(
        string localFilePath,
        Action<TransferProgress>? onProgress = null,
        CancellationToken cancellationToken = default
    )
    {
        if (localFilePath == null) throw new ArgumentNullException(nameof(localFilePath));

        var file = new FileInfo(localFilePath);
        if (!file.Exists)
            throw new FileNotFoundException("File to serve was not found", localFilePath);

        using var stream = file.OpenRead();
        return await this
            .AcceptDownload(stream, file.Length, onProgress, cancellationToken)
            .ConfigureAwait(false);
    }


    /// <summary>
    /// Accepts a download request and streams <paramref name="source"/> back to the peer.
    /// </summary>
    /// <param name="source">The stream to send. Not disposed by this method.</param>
    /// <param name="length">Exact number of bytes that will be sent. The peer uses this for percent complete.</param>
    /// <param name="onProgress">Optional progress callback - percent complete, throughput, and ETA.</param>
    /// <param name="cancellationToken">Token used to cancel the transfer.</param>
    /// <returns>The transfer outcome.</returns>
    public async Task<L2CapTransferResult> AcceptDownload(
        Stream source,
        long length,
        Action<TransferProgress>? onProgress = null,
        CancellationToken cancellationToken = default
    )
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative");
        this.Answer(L2CapTransferType.Download);

        await this.channel
            .WriteFrame(L2CapProtocol.Accept(this.FileName, length), cancellationToken)
            .ConfigureAwait(false);

        var reporter = await this.channel
            .SendBody(this.reader, source, length, this.options, onProgress, cancellationToken)
            .ConfigureAwait(false);

        var ack = await this.reader.Expect(L2CapOpCode.Ack, this.options, cancellationToken).ConfigureAwait(false);
        var received = ack.ReadAck();
        if (received != length)
        {
            this.reader.Fault();
            throw new L2CapTransferException(L2CapTransferError.ProtocolError, $"Peer acknowledged {received} bytes but {length} were sent");
        }

        return new L2CapTransferResult(L2CapTransferType.Download, this.FileName, reporter.BytesTransferred, reporter.Elapsed);
    }


    /// <summary>
    /// Refuses the request. The peer's call fails with a matching <see cref="L2CapTransferException"/>
    /// and the channel stays usable for further requests.
    /// </summary>
    /// <param name="error">The reason to report.</param>
    /// <param name="message">Optional human readable detail.</param>
    /// <param name="cancellationToken">Token used to cancel the write.</param>
    public Task Reject(
        L2CapTransferError error = L2CapTransferError.NotPermitted,
        string? message = null,
        CancellationToken cancellationToken = default
    )
    {
        this.Answer(null);
        return this.channel.WriteFrame(L2CapProtocol.Error(error, message), cancellationToken);
    }


    void Answer(L2CapTransferType? expected)
    {
        if (expected != null && expected != this.Type)
            throw new InvalidOperationException($"This is a {this.Type} request - answer it with the matching Accept{this.Type} overload");

        if (Interlocked.Exchange(ref this.answered, 1) == 1)
            throw new InvalidOperationException("This request has already been answered");
    }
}
