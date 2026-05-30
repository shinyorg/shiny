using System;

namespace Shiny.BluetoothLE;


/// <summary>
/// Represents the progress of an in-flight L2CAP transfer.
/// Mirrors the shape of <c>Shiny.Net.Http.TransferProgress</c> so consumers
/// familiar with the HTTP transfer surface get an identical mental model.
/// </summary>
/// <param name="BytesPerSecond">Estimated current throughput in bytes per second.</param>
/// <param name="BytesToTransfer">Total bytes expected, or null when unknown.</param>
/// <param name="BytesTransferred">Bytes transferred so far.</param>
public record TransferProgress(
    long BytesPerSecond,
    long? BytesToTransfer,
    long BytesTransferred
)
{
    /// <summary>
    /// Gets an empty progress instance (zero bytes, zero throughput).
    /// </summary>
    public static TransferProgress Empty { get; } = new(0, 0, 0);

    /// <summary>
    /// Gets a value indicating whether the total transfer size is known.
    /// </summary>
    public bool IsDeterministic => this.BytesToTransfer != null;


    double? percentComplete;

    /// <summary>
    /// Gets the percent complete as a value between 0.0 and 1.0, or -1 when not deterministic.
    /// </summary>
    public double PercentComplete
    {
        get
        {
            if (!this.IsDeterministic)
                return -1;

            if (this.percentComplete == null)
                this.percentComplete = Math.Round((double)this.BytesTransferred / this.BytesToTransfer!.Value, 2);

            return this.percentComplete.Value;
        }
    }


    TimeSpan? estimate;

    /// <summary>
    /// Gets the estimated time remaining based on current throughput, or <see cref="TimeSpan.Zero"/> when unknown.
    /// </summary>
    public TimeSpan EstimatedTimeRemaining
    {
        get
        {
            if (this.estimate == null)
            {
                if (this.BytesToTransfer == null || this.BytesPerSecond == 0)
                {
                    this.estimate = TimeSpan.Zero;
                }
                else
                {
                    var bytesRemaining = this.BytesToTransfer.Value - this.BytesTransferred;
                    this.estimate = TimeSpan.FromSeconds(bytesRemaining / this.BytesPerSecond);
                }
            }
            return this.estimate!.Value;
        }
    }
}
