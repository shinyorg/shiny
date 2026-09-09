using System;
using System.Collections.Generic;
using System.Linq;

namespace Shiny.Beacons.Infrastructure;


/// <summary>
/// A region whose state just changed.
/// </summary>
/// <param name="Region">The region.</param>
/// <param name="State">The state it moved into.</param>
public record BeaconRegionTransition(BeaconRegion Region, BeaconRegionState State);


/// <summary>
/// Tracks which beacon regions the device is inside, from a stream of beacon sightings.
/// </summary>
/// <remarks>
/// <para>
/// BLE gives no "left the area" event - all a scanner ever learns is that a beacon was seen. Exits
/// therefore have to be inferred from silence, which is what <see cref="Evaluate"/> does: a region
/// that has gone <see cref="BeaconRangingOptions.RegionExitTimeout"/> without a matching sighting
/// is considered exited.
/// </para>
/// <para>
/// This carries no platform or Bluetooth dependency so that the state machine - the part that has
/// historically been wrong - can be tested directly against a fake clock.
/// </para>
/// </remarks>
public class BeaconRegionMonitor(BeaconRangingOptions options)
{
    sealed class RegionState(BeaconRegion region)
    {
        public BeaconRegion Region { get; set; } = region;
        public BeaconRegionState Status { get; set; } = BeaconRegionState.Unknown;
        public DateTimeOffset? LastSeen { get; set; }
    }

    readonly Dictionary<string, RegionState> states = new();
    readonly object syncLock = new();


    /// <summary>
    /// Replaces the monitored set, preserving the state of regions that are staying.
    /// </summary>
    /// <param name="regions">The regions that should be monitored from now on.</param>
    public void SetRegions(IEnumerable<BeaconRegion> regions)
    {
        lock (this.syncLock)
        {
            var incoming = regions.ToList();
            var keep = new HashSet<string>(incoming.Select(x => x.Identifier));

            foreach (var stale in this.states.Keys.Where(x => !keep.Contains(x)).ToList())
                this.states.Remove(stale);

            foreach (var region in incoming)
            {
                // an identifier that is already tracked keeps its state, so reconciling on startup
                // does not re-fire an entry the delegate has already been told about
                if (this.states.TryGetValue(region.Identifier, out var existing))
                    existing.Region = region;
                else
                    this.states.Add(region.Identifier, new RegionState(region));
            }
        }
    }


    /// <summary>
    /// Starts monitoring a region, or updates it in place when the identifier is already tracked.
    /// </summary>
    /// <param name="region">The region to monitor.</param>
    public void AddRegion(BeaconRegion region)
    {
        lock (this.syncLock)
        {
            // indexer, not Add - re-registering an identifier is a legitimate way to change a
            // region's bounds, and the pre-revival module crashed with a duplicate key here
            if (this.states.TryGetValue(region.Identifier, out var existing))
                existing.Region = region;
            else
                this.states[region.Identifier] = new RegionState(region);
        }
    }


    /// <summary>
    /// Stops monitoring the region with the supplied identifier.
    /// </summary>
    /// <param name="identifier">The region identifier.</param>
    public void RemoveRegion(string identifier)
    {
        lock (this.syncLock)
            this.states.Remove(identifier);
    }


    /// <summary>
    /// Stops monitoring every region.
    /// </summary>
    public void Clear()
    {
        lock (this.syncLock)
            this.states.Clear();
    }


    /// <summary>
    /// The number of regions being tracked.
    /// </summary>
    public int Count
    {
        get
        {
            lock (this.syncLock)
                return this.states.Count;
        }
    }


    /// <summary>
    /// The current state of a region, or <see cref="BeaconRegionState.Unknown"/> when it is not
    /// tracked or has not been evaluated yet.
    /// </summary>
    /// <param name="identifier">The region identifier.</param>
    public BeaconRegionState GetState(string identifier)
    {
        lock (this.syncLock)
            return this.states.TryGetValue(identifier, out var state) ? state.Status : BeaconRegionState.Unknown;
    }


    /// <summary>
    /// Records that a beacon was seen and returns any regions that were entered as a result.
    /// </summary>
    /// <param name="beacon">The beacon's identity.</param>
    /// <returns>The transitions to report. Empty when nothing changed.</returns>
    public IReadOnlyList<BeaconRegionTransition> Report(BeaconIdentity beacon)
    {
        var now = options.TimeProvider.GetUtcNow();
        List<BeaconRegionTransition>? transitions = null;

        lock (this.syncLock)
        {
            foreach (var state in this.states.Values)
            {
                if (!state.Region.IsBeaconInRegion(beacon.Uuid, beacon.Major, beacon.Minor))
                    continue;

                state.LastSeen = now;

                // NOTE: Unknown counts as an entry. The pre-revival module seeded the first
                // sighting as "already inside" and then tested for a change, so a region entered
                // from cold never raised an event at all.
                if (state.Status != BeaconRegionState.Entered)
                {
                    state.Status = BeaconRegionState.Entered;

                    if (state.Region.NotifyOnEntry)
                        (transitions ??= []).Add(new BeaconRegionTransition(state.Region, BeaconRegionState.Entered));
                }
            }
        }

        return transitions ?? (IReadOnlyList<BeaconRegionTransition>)Array.Empty<BeaconRegionTransition>();
    }


    /// <summary>
    /// Ages out regions that have gone quiet and returns any that were exited as a result.
    /// Call this on <see cref="BeaconRangingOptions.RegionEvaluationInterval"/>.
    /// </summary>
    /// <returns>The transitions to report. Empty when nothing changed.</returns>
    public IReadOnlyList<BeaconRegionTransition> Evaluate()
    {
        var cutoff = options.TimeProvider.GetUtcNow().Subtract(options.RegionExitTimeout);
        List<BeaconRegionTransition>? transitions = null;

        lock (this.syncLock)
        {
            foreach (var state in this.states.Values)
            {
                if (state.Status != BeaconRegionState.Entered)
                    continue;

                if (state.LastSeen > cutoff)
                    continue;

                state.Status = BeaconRegionState.Exited;

                if (state.Region.NotifyOnExit)
                    (transitions ??= []).Add(new BeaconRegionTransition(state.Region, BeaconRegionState.Exited));
            }
        }

        return transitions ?? (IReadOnlyList<BeaconRegionTransition>)Array.Empty<BeaconRegionTransition>();
    }
}
