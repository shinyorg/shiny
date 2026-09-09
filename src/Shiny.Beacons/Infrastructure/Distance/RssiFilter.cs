using System;
using System.Collections.Generic;

namespace Shiny.Beacons.Infrastructure;


/// <summary>
/// A moving window of RSSI samples with the outliers trimmed off before averaging.
/// </summary>
/// <remarks>
/// <para>
/// BLE signal strength is extremely noisy - consecutive advertisements from a stationary beacon
/// routinely differ by 10 dBm or more, which a raw distance calculation turns into several metres of
/// jitter. Feeding an estimator one advertisement at a time, as the pre-revival module did, is the
/// single biggest source of bad distance readings on Android.
/// </para>
/// <para>
/// This keeps every sample inside a time window, sorts them, discards a fraction from each end to
/// drop reflections and dropouts, and averages what is left. It is the approach AltBeacon's
/// <c>RunningAverageRssiFilter</c> takes, and it is what makes readings settle.
/// </para>
/// <para>This type is not thread safe; callers synchronize access.</para>
/// </remarks>
public class RssiFilter
{
    readonly record struct Sample(DateTimeOffset Timestamp, int Rssi);

    readonly LinkedList<Sample> samples = new();
    readonly TimeSpan window;
    readonly double trimFraction;
    readonly TimeProvider timeProvider;


    /// <summary>
    /// Creates a filter.
    /// </summary>
    /// <param name="window">How far back samples are kept.</param>
    /// <param name="trimFraction">
    /// The fraction of samples discarded from each end of the sorted set, between 0 and 0.4.
    /// </param>
    /// <param name="timeProvider">The clock used to age samples out. Defaults to the system clock.</param>
    public RssiFilter(TimeSpan window, double trimFraction = 0.1, TimeProvider? timeProvider = null)
    {
        if (window <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window), "The filter window must be positive");

        if (trimFraction is < 0 or > 0.4)
            throw new ArgumentOutOfRangeException(nameof(trimFraction), "The trim fraction must be between 0 and 0.4");

        this.window = window;
        this.trimFraction = trimFraction;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }


    /// <summary>
    /// The number of samples currently inside the window.
    /// </summary>
    public int Count
    {
        get
        {
            this.Prune();
            return this.samples.Count;
        }
    }


    /// <summary>
    /// The most recent raw sample, ignoring the filter. Null when the window is empty.
    /// </summary>
    public int? Latest
    {
        get
        {
            this.Prune();
            return this.samples.Last?.Value.Rssi;
        }
    }


    /// <summary>
    /// When the most recent sample arrived. Null when the window is empty.
    /// </summary>
    public DateTimeOffset? LastSeen
    {
        get
        {
            this.Prune();
            return this.samples.Last?.Value.Timestamp;
        }
    }


    /// <summary>
    /// Records a signal strength reading.
    /// </summary>
    /// <param name="rssi">The observed RSSI in dBm.</param>
    public void Add(int rssi)
    {
        this.samples.AddLast(new Sample(this.timeProvider.GetUtcNow(), rssi));
        this.Prune();
    }


    /// <summary>
    /// The trimmed mean of the samples in the window, or null when there are none.
    /// </summary>
    public double? Value
    {
        get
        {
            this.Prune();
            var count = this.samples.Count;
            if (count == 0)
                return null;

            if (count <= 2)
            {
                var total = 0d;
                foreach (var sample in this.samples)
                    total += sample.Rssi;

                return total / count;
            }

            var sorted = new int[count];
            var index = 0;
            foreach (var sample in this.samples)
                sorted[index++] = sample.Rssi;

            Array.Sort(sorted);

            // Keep at least one sample no matter how aggressive the trim fraction is
            var trim = (int)(count * this.trimFraction);
            if (trim * 2 >= count)
                trim = (count - 1) / 2;

            var sum = 0L;
            for (var i = trim; i < count - trim; i++)
                sum += sorted[i];

            return sum / (double)(count - (trim * 2));
        }
    }


    /// <summary>
    /// Discards every sample.
    /// </summary>
    public void Clear() => this.samples.Clear();


    void Prune()
    {
        var cutoff = this.timeProvider.GetUtcNow().Subtract(this.window);

        while (this.samples.First is { } first && first.Value.Timestamp < cutoff)
            this.samples.RemoveFirst();
    }
}
