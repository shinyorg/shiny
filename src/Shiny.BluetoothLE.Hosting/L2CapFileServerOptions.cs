using System;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Configuration for the directory backed L2CAP file server started by
/// <see cref="L2CapFileServerExtensions.OpenL2CapFileServer(IBleHostingManager, L2CapFileServerOptions)"/>.
/// </summary>
public class L2CapFileServerOptions
{
    /// <summary>
    /// Creates a new instance serving files out of <paramref name="rootDirectory"/>.
    /// </summary>
    /// <param name="rootDirectory">The directory downloads are read from and uploads are written to. Created if missing.</param>
    public L2CapFileServerOptions(string rootDirectory)
        => this.RootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));


    /// <summary>
    /// Gets the directory downloads are served from and uploads are written to.
    /// Requested names are resolved under this directory and anything escaping it is refused.
    /// </summary>
    public string RootDirectory { get; }

    /// <summary>
    /// Requires an encrypted/authenticated channel (Android API 29+). Defaults to false.
    /// </summary>
    public bool Secure { get; set; }

    /// <summary>
    /// Accepts files pushed by connected peers. Defaults to true.
    /// </summary>
    public bool AllowUploads { get; set; } = true;

    /// <summary>
    /// Serves files requested by connected peers. Defaults to true.
    /// </summary>
    public bool AllowDownloads { get; set; } = true;

    /// <summary>
    /// Allows an upload to replace a file that already exists. Defaults to true.
    /// When false, an upload naming an existing file is refused with <see cref="L2CapTransferError.NotPermitted"/>.
    /// </summary>
    public bool OverwriteExistingUploads { get; set; } = true;

    /// <summary>
    /// Largest upload to accept, in bytes. Anything larger is refused with
    /// <see cref="L2CapTransferError.TooLarge"/> before a single body byte is read. Null means no limit.
    /// </summary>
    public long? MaxUploadSize { get; set; }

    /// <summary>
    /// Buffer size, progress interval, and idle timeout applied to every transfer. Defaults to <see cref="L2CapTransferOptions.Default"/>.
    /// </summary>
    public L2CapTransferOptions Transfer { get; set; } = L2CapTransferOptions.Default;

    /// <summary>
    /// Optional per-request authorization hook, called after the built-in checks pass.
    /// Return false to refuse the request with <see cref="L2CapTransferError.NotPermitted"/>.
    /// </summary>
    public Func<L2CapFileRequest, bool>? Authorize { get; set; }

    /// <summary>
    /// Raised as a transfer streams, at <see cref="L2CapTransferOptions.ProgressInterval"/>.
    /// </summary>
    public Action<L2CapFileTransferEvent>? OnProgress { get; set; }

    /// <summary>
    /// Raised once a transfer finishes successfully.
    /// </summary>
    public Action<L2CapFileServerResult>? OnCompleted { get; set; }

    /// <summary>
    /// Raised when a request is refused or a transfer fails. The request is null when the failure
    /// was not tied to one (for example the channel dropped while idle).
    /// </summary>
    public Action<L2CapFileRequest?, Exception>? OnError { get; set; }
}


/// <summary>
/// Progress snapshot for a transfer running on the file server.
/// </summary>
/// <param name="PeerIdentifier">The remote peer identifier.</param>
/// <param name="Type">The kind of transfer the peer initiated.</param>
/// <param name="FileName">The file name the peer requested.</param>
/// <param name="Progress">Percent complete, throughput, and ETA.</param>
public record L2CapFileTransferEvent(
    string PeerIdentifier,
    L2CapTransferType Type,
    string FileName,
    TransferProgress Progress
);


/// <summary>
/// A completed transfer on the file server.
/// </summary>
/// <param name="PeerIdentifier">The remote peer identifier.</param>
/// <param name="LocalFilePath">The local file that was read from or written to.</param>
/// <param name="Result">Bytes moved, elapsed time, and average throughput.</param>
public record L2CapFileServerResult(
    string PeerIdentifier,
    string LocalFilePath,
    L2CapTransferResult Result
);
