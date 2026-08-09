using System;
using System.Diagnostics;

namespace Shiny.BluetoothLE.Infrastructure;


/// <summary>
/// Accumulates transferred bytes and emits <see cref="TransferProgress"/> on an interval.
/// </summary>
/// <remarks>
/// Throughput is measured over the current reporting window rather than the whole transfer, so the
/// number tracks what the link is doing right now (matching how Shiny.Net.Http reports it).
/// </remarks>
sealed class L2CapProgressReporter
{
    readonly long? total;
    readonly TimeSpan interval;
    readonly Action<TransferProgress>? onProgress;
    readonly Stopwatch elapsed = Stopwatch.StartNew();
    readonly Stopwatch window = Stopwatch.StartNew();
    long windowBytes;


    public L2CapProgressReporter(long? total, TimeSpan interval, Action<TransferProgress>? onProgress)
    {
        this.total = total;
        this.interval = interval <= TimeSpan.Zero ? TimeSpan.FromSeconds(2) : interval;
        this.onProgress = onProgress;
    }


    /// <summary>Total bytes seen so far.</summary>
    public long BytesTransferred { get; private set; }

    /// <summary>Time since the reporter was created.</summary>
    public TimeSpan Elapsed => this.elapsed.Elapsed;


    public void Add(int bytes)
    {
        this.BytesTransferred += bytes;
        this.windowBytes += bytes;

        if (this.onProgress == null || this.window.Elapsed < this.interval)
            return;

        var seconds = this.window.Elapsed.TotalSeconds;
        var bytesPerSecond = seconds <= 0 ? 0 : (long)(this.windowBytes / seconds);
        this.onProgress(new TransferProgress(bytesPerSecond, this.total, this.BytesTransferred));

        this.windowBytes = 0;
        this.window.Restart();
    }


    /// <summary>
    /// Emits a final progress event carrying the average throughput for the whole transfer.
    /// </summary>
    public TransferProgress Complete()
    {
        this.elapsed.Stop();
        var seconds = this.elapsed.Elapsed.TotalSeconds;
        var bytesPerSecond = seconds <= 0 ? 0 : (long)(this.BytesTransferred / seconds);

        var progress = new TransferProgress(bytesPerSecond, this.total ?? this.BytesTransferred, this.BytesTransferred);
        this.onProgress?.Invoke(progress);
        return progress;
    }
}
