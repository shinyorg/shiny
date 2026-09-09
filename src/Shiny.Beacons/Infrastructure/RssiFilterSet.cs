using System;
using System.Collections.Generic;
using System.Linq;

namespace Shiny.Beacons.Infrastructure;


/// <summary>
/// Keeps one <see cref="RssiFilter"/> per transmitter, keyed by whatever identifies it.
/// </summary>
/// <remarks>
/// Averaging only works per beacon - mixing samples from two transmitters would smear their
/// distances into each other. This owns that bookkeeping, including dropping the filters of
/// beacons that have gone out of range so a long scan does not grow without bound.
/// </remarks>
public class RssiFilterSet(BeaconRangingOptions options)
{
    readonly Dictionary<string, RssiFilter> filters = new();
    readonly object syncLock = new();


    /// <summary>
    /// Records a sample and returns the filtered signal strength for that transmitter.
    /// </summary>
    /// <param name="key">Whatever identifies the transmitter - a beacon identity or peripheral id.</param>
    /// <param name="rssi">The observed RSSI in dBm.</param>
    /// <returns>The trimmed mean over the filter window.</returns>
    public double Add(string key, int rssi)
    {
        lock (this.syncLock)
        {
            if (!this.filters.TryGetValue(key, out var filter))
            {
                filter = new RssiFilter(options.RssiFilterWindow, options.RssiTrimFraction, options.TimeProvider);
                this.filters.Add(key, filter);
            }

            filter.Add(rssi);

            // the filter cannot be empty immediately after an Add, so the fallback never fires in
            // practice - it is here so the signature does not have to be nullable
            return filter.Value ?? rssi;
        }
    }


    /// <summary>
    /// Drops the filter for a transmitter.
    /// </summary>
    /// <param name="key">The transmitter key.</param>
    public void Remove(string key)
    {
        lock (this.syncLock)
            this.filters.Remove(key);
    }


    /// <summary>
    /// Drops every filter whose window has emptied - the transmitter has not been heard from for
    /// longer than <see cref="BeaconRangingOptions.RssiFilterWindow"/>.
    /// </summary>
    public void PruneIdle()
    {
        lock (this.syncLock)
        {
            foreach (var key in this.filters.Where(x => x.Value.Count == 0).Select(x => x.Key).ToList())
                this.filters.Remove(key);
        }
    }


    /// <summary>
    /// Drops every filter.
    /// </summary>
    public void Clear()
    {
        lock (this.syncLock)
            this.filters.Clear();
    }
}
