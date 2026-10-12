using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores;

namespace Shiny.Locations;


/// <summary>
/// Turns GPS positions into region transitions: queries each registered set, diffs the result against the
/// region the device was last known to be in, and raises enter/exit through the app's delegate.
/// <para>
/// The "last region" per set is persisted, so a device that crosses a boundary, has its process killed, and
/// is relaunched by the OS does not replay an entry it already raised.
/// </para>
/// </summary>
public class RegionEvaluator
{
    const string KeyPrefix = "regiongeofence.set.";

    readonly IReadOnlyList<ResolvedSet> sets;
    readonly IKeyValueStore state;
    readonly IRegionGeofenceDelegate geofenceDelegate;
    readonly ILogger logger;

    // Mirrors the persisted ids, but carries the region too - the persisted key only survives the id, and
    // an exit event should describe the region without a round-trip when the process hasn't been recycled.
    readonly Dictionary<string, RegionMatch> lastMatches = new();


    /// <summary>Creates the evaluator and resolves every set's source. Registered as a singleton by <c>AddRegionGeofencing</c>.</summary>
    public RegionEvaluator(
        IServiceProvider services,
        [FromKeyedServices(StoreKeys.Default)] IKeyValueStore state,
        IRegionGeofenceDelegate geofenceDelegate,
        RegionGeofenceConfig config,
        ILogger<RegionEvaluator> logger
    )
    {
        this.sets = config
            .RegionSets
            .Select(x => new ResolvedSet(x.Name, x.SourceFactory(services)))
            .ToList();

        this.state = state;
        this.geofenceDelegate = geofenceDelegate;
        this.logger = logger;
    }


    /// <summary>
    /// Evaluates a position against every registered set and raises the resulting transitions.
    /// </summary>
    /// <param name="position">The position to evaluate.</param>
    /// <param name="cancellationToken">Cancels the pending queries.</param>
    /// <returns>The transitions that were raised, in the order they were delivered.</returns>
    public async Task<IReadOnlyList<RegionChange>> Evaluate(Position position, CancellationToken cancellationToken = default)
    {
        var changes = new List<RegionChange>();

        foreach (var set in this.sets)
            await this.EvaluateSet(set, position, changes, cancellationToken).ConfigureAwait(false);

        foreach (var change in changes)
            await this.Fire(change).ConfigureAwait(false);

        return changes;
    }


    /// <summary>
    /// Resolves the region the position falls in for every set without raising events or recording state.
    /// </summary>
    /// <param name="position">The position to resolve.</param>
    /// <param name="cancellationToken">Cancels the pending queries.</param>
    public async Task<IReadOnlyList<CurrentRegion>> Current(Position position, CancellationToken cancellationToken = default)
    {
        var results = new List<CurrentRegion>(this.sets.Count);

        foreach (var set in this.sets)
        {
            var match = await set.Source.FindRegion(position, cancellationToken).ConfigureAwait(false);
            results.Add(new CurrentRegion(set.Name, match?.Id, match?.Name, match?.Region, match?.DistanceMeters ?? 0));
        }
        return results;
    }


    /// <summary>Forgets which region the device was in for every set, so the next reading re-enters from scratch.</summary>
    public void Reset()
    {
        foreach (var set in this.sets)
            this.state.Remove(KeyPrefix + set.Name);

        this.lastMatches.Clear();
    }


    async Task EvaluateSet(ResolvedSet set, Position position, List<RegionChange> changes, CancellationToken cancellationToken)
    {
        RegionMatch? current;
        try
        {
            current = await set.Source.FindRegion(position, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // One broken set (a bad query, a dropped connection) must not stop the others from
            // reporting - and must not poison this set's last-known region either.
            this.logger.LogError(ex, "Geofence query failed for region set '{RegionSet}'", set.Name);
            return;
        }

        var previousId = this.GetPersistedId(set.Name);
        if (previousId == current?.Id)
            return;

        if (previousId != null)
        {
            var previous = await this.ResolvePrevious(set, previousId, cancellationToken).ConfigureAwait(false);
            changes.Add(new RegionChange(
                set.Name,
                previousId,
                previous?.Name,
                previous?.Region,
                Entered: false,
                position
            ));
        }

        if (current != null)
        {
            changes.Add(new RegionChange(
                set.Name,
                current.Id,
                current.Name,
                current.Region,
                Entered: true,
                position
            ));
        }

        this.SetCurrent(set.Name, current);
    }


    async Task Fire(RegionChange change)
    {
        try
        {
            await this.geofenceDelegate.OnRegionChanged(change).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(
                ex,
                "Geofence delegate threw handling {Transition} of '{RegionId}' in set '{RegionSet}'",
                change.Entered ? "entry" : "exit",
                change.RegionId,
                change.RegionSet
            );
        }
    }


    async Task<RegionMatch?> ResolvePrevious(ResolvedSet set, string previousId, CancellationToken cancellationToken)
    {
        if (this.lastMatches.TryGetValue(set.Name, out var cached) && cached.Id == previousId)
            return cached;

        // The id outlived the process (or the cache) - read the region back so the exit still describes it.
        try
        {
            return await set.Source.GetRegion(previousId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not re-read exited region '{RegionId}' in set '{RegionSet}'", previousId, set.Name);
            return null;
        }
    }


    string? GetPersistedId(string setName)
    {
        var value = this.state.Get<string>(KeyPrefix + setName);
        return String.IsNullOrWhiteSpace(value) ? null : value;
    }


    void SetCurrent(string setName, RegionMatch? match)
    {
        if (match == null)
        {
            this.state.Remove(KeyPrefix + setName);
            this.lastMatches.Remove(setName);
        }
        else
        {
            this.state.Set(KeyPrefix + setName, match.Id);
            this.lastMatches[setName] = match;
        }
    }


    record ResolvedSet(string Name, IRegionSource Source);
}
