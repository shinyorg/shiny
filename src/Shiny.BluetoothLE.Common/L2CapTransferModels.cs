using System;

namespace Shiny.BluetoothLE;


/// <summary>
/// The direction of an L2CAP file transfer, as seen by the peer that initiated it.
/// </summary>
public enum L2CapTransferType
{
    /// <summary>The initiator is sending a file to the remote peer.</summary>
    Upload,

    /// <summary>The initiator is requesting a file from the remote peer.</summary>
    Download
}


/// <summary>
/// Reason a transfer was refused or failed. Sent across the wire so both peers agree on the cause.
/// </summary>
public enum L2CapTransferError : byte
{
    /// <summary>The cause was not reported.</summary>
    Unknown = 0,

    /// <summary>The requested file does not exist on the remote peer.</summary>
    NotFound = 1,

    /// <summary>The remote peer refused the operation (uploads or downloads disabled, name rejected).</summary>
    NotPermitted = 2,

    /// <summary>The upload is larger than the remote peer will accept.</summary>
    TooLarge = 3,

    /// <summary>The remote peer hit an IO error reading or writing the file.</summary>
    IoError = 4,

    /// <summary>The peers fell out of sync, or the channel is not speaking this protocol.</summary>
    ProtocolError = 5,

    /// <summary>The remote peer cancelled the transfer.</summary>
    Cancelled = 6
}


/// <summary>
/// Raised when an L2CAP file transfer is refused, fails, or the peers fall out of sync.
/// </summary>
public class L2CapTransferException : BleException
{
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="error">The reported cause.</param>
    /// <param name="message">The error description.</param>
    public L2CapTransferException(L2CapTransferError error, string message) : base(message)
        => this.Error = error;


    /// <summary>
    /// Creates a new instance wrapping an underlying failure.
    /// </summary>
    /// <param name="error">The reported cause.</param>
    /// <param name="message">The error description.</param>
    /// <param name="innerException">The underlying exception.</param>
    public L2CapTransferException(L2CapTransferError error, string message, Exception innerException) : base(message, innerException)
        => this.Error = error;


    /// <summary>
    /// Gets the reported cause.
    /// </summary>
    public L2CapTransferError Error { get; }
}


/// <summary>
/// Tuning knobs for an L2CAP file transfer.
/// </summary>
public record L2CapTransferOptions
{
    /// <summary>
    /// Gets the shared default options - 4KB buffer, progress every 2 seconds, 30 second idle timeout.
    /// </summary>
    public static L2CapTransferOptions Default { get; } = new();

    /// <summary>
    /// Bytes read/written per chunk. Defaults to 4096. Larger values reduce syscalls but coarsen progress reporting.
    /// </summary>
    public int BufferSize { get; init; } = 4096;

    /// <summary>
    /// How often <c>onProgress</c> fires while the body is streaming. Defaults to 2 seconds.
    /// A completion event is always raised regardless of this interval.
    /// </summary>
    public TimeSpan ProgressInterval { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How long a read may stall before the transfer is abandoned. Defaults to 30 seconds.
    /// Use <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to wait indefinitely.
    /// </summary>
    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromSeconds(30);


    internal void Assert()
    {
        if (this.BufferSize < 1)
            throw new ArgumentOutOfRangeException(nameof(this.BufferSize), "BufferSize must be at least 1");
    }
}


/// <summary>
/// The outcome of a completed L2CAP file transfer.
/// </summary>
/// <param name="Type">The kind of transfer, as named by the peer that initiated it. Both peers report the same value for a given transfer.</param>
/// <param name="FileName">The file name agreed with the remote peer.</param>
/// <param name="BytesTransferred">Total bytes moved.</param>
/// <param name="Elapsed">Wall clock duration of the transfer.</param>
public record L2CapTransferResult(
    L2CapTransferType Type,
    string FileName,
    long BytesTransferred,
    TimeSpan Elapsed
)
{
    /// <summary>
    /// Gets the average throughput in bytes per second across the whole transfer.
    /// </summary>
    public long BytesPerSecond => this.Elapsed.TotalSeconds <= 0
        ? 0
        : (long)(this.BytesTransferred / this.Elapsed.TotalSeconds);
}
