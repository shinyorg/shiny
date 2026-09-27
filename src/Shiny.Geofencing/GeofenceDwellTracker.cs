using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Locations;


/// <summary>
/// Persisted record of a stay inside a region that has a dwell time - survives app restarts.
/// </summary>
/// <param name="Identifier">The region identifier.</param>
/// <param name="EnteredAt">When the device entered (or monitoring started while already inside).</param>
/// <param name="Fired">Whether Dwelling was already reported for this stay.</param>
record GeofenceDwellEntry(string Identifier, DateTimeOffset EnteredAt, bool Fired = false) : IRepositoryEntity;


/// <summary>
/// Dwell detection for platforms without a native dwell transition (iOS, Windows, GPS-direct).
/// Needs no location updates of its own: the entry time is recorded (and persisted) on entry, and dwell is reported
/// whichever comes first:
/// <list type="bullet">
/// <item>a timer, if the app is still running when the dwell time elapses - the region state is re-checked first</item>
/// <item>the exit - if the time between entry and exit reached the dwell time, Dwelling is reported just before the exit</item>
/// </list>
/// iOS suspends apps in the background, so there the exit is usually what reports it.
/// </summary>
sealed class GeofenceDwellTracker(
    IRepository repository,
    ILogger logger,
    Func<GeofenceRegion, CancellationToken, Task<GeofenceState>> getState,
    Func<GeofenceRegion, Task> onDwell,
    TimeProvider? timeProvider = null
)
{
    static readonly TimeSpan verifyTimeout = TimeSpan.FromSeconds(30);

    readonly TimeProvider time = timeProvider ?? TimeProvider.System;
    readonly ConcurrentDictionary<string, CancellationTokenSource> timers = new();
    readonly SemaphoreSlim fireLock = new(1, 1);


    public bool HasPending => !this.timers.IsEmpty;


    /// <summary>
    /// Records the entry - an existing entry is kept, so a duplicate or replayed enter never restarts the clock.
    /// </summary>
    /// <param name="region">The region entered.</param>
    /// <param name="at">When the platform saw the entry - defaults to now.</param>
    public void Entered(GeofenceRegion region, DateTimeOffset? at = null)
    {
        if (region.DwellTime == null)
            return;

        var entry = repository.Get<GeofenceDwellEntry>(region.Identifier);
        if (entry == null)
        {
            entry = new GeofenceDwellEntry(region.Identifier, at ?? this.time.GetUtcNow());
            repository.Set(entry);
        }
        this.Schedule(region, entry);
    }


    /// <summary>
    /// Ends the stay. If it lasted the region's dwell time and Dwelling was not reported yet, it is reported now -
    /// await this before reporting the exit so the delegate sees Dwelling first.
    /// </summary>
    /// <param name="region">The region exited.</param>
    /// <param name="at">When the platform saw the exit - defaults to now.</param>
    public async Task Exited(GeofenceRegion region, DateTimeOffset? at = null)
    {
        this.Cancel(region.Identifier);
        if (region.DwellTime is not { } dwellTime)
            return;

        await this.fireLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var entry = repository.Get<GeofenceDwellEntry>(region.Identifier);
            if (entry != null)
            {
                repository.Remove<GeofenceDwellEntry>(region.Identifier);

                var stayed = (at ?? this.time.GetUtcNow()) - entry.EnteredAt;
                if (!entry.Fired && stayed >= dwellTime)
                {
                    logger.LogDebug("Geofence {Identifier} dwell calculated on exit - stayed {Stayed}", region.Identifier, stayed);
                    await onDwell(region).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            this.fireLock.Release();
        }
    }


    /// <summary>
    /// Forgets the region without reporting anything - for stop monitoring.
    /// </summary>
    public void Remove(string identifier)
    {
        this.Cancel(identifier);
        repository.Remove<GeofenceDwellEntry>(identifier);
    }


    public void Clear()
    {
        foreach (var id in this.timers.Keys)
            this.Cancel(id);

        repository.Clear<GeofenceDwellEntry>();
    }


    /// <summary>
    /// Reschedules stays that were in progress when the app last stopped. Overdue ones are re-checked immediately.
    /// </summary>
    public void Restore()
    {
        foreach (var entry in repository.GetAll<GeofenceDwellEntry>())
        {
            var region = repository.Get<GeofenceRegion>(entry.Identifier);
            if (region?.DwellTime == null)
                repository.Remove<GeofenceDwellEntry>(entry.Identifier);
            else
                this.Schedule(region, entry);
        }
    }


    /// <summary>
    /// Re-checks every pending stay now - call when the app returns to the foreground. Timers don't advance while
    /// iOS has the process suspended, so one that was due during the suspension would otherwise still be waiting.
    /// </summary>
    public void Reevaluate()
    {
        foreach (var id in this.timers.Keys)
            this.Cancel(id);

        this.Restore();
    }


    void Schedule(GeofenceRegion region, GeofenceDwellEntry entry)
    {
        if (entry.Fired || this.timers.ContainsKey(region.Identifier))
            return;

        var cts = new CancellationTokenSource();
        if (!this.timers.TryAdd(region.Identifier, cts))
        {
            cts.Dispose();
            return;
        }
        var remaining = entry.EnteredAt + region.DwellTime!.Value - this.time.GetUtcNow();
        _ = this.Run(region, remaining, cts);
    }


    async Task Run(GeofenceRegion region, TimeSpan remaining, CancellationTokenSource cts)
    {
        try
        {
            if (remaining > TimeSpan.Zero)
                await Task.Delay(remaining, this.time, cts.Token).ConfigureAwait(false);

            GeofenceState state;
            using (var verifyCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token))
            {
                verifyCts.CancelAfter(verifyTimeout);
                state = await getState(region, verifyCts.Token).ConfigureAwait(false);
            }
            this.Cancel(region.Identifier, cts);

            if (state != GeofenceState.Entered)
            {
                // the exit (already seen or still to come) decides whether the stay counted
                logger.LogDebug("Geofence {Identifier} dwell time elapsed but region state is {State} - leaving it to the exit", region.Identifier, state);
            }
            else
            {
                await this.fireLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    // the exit may have run while the state was being checked
                    var entry = repository.Get<GeofenceDwellEntry>(region.Identifier);
                    if (entry is { Fired: false })
                    {
                        repository.Set(entry with { Fired = true });
                        await onDwell(region).ConfigureAwait(false);
                    }
                }
                finally
                {
                    this.fireLock.Release();
                }
            }
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // exited, stopped, cleared or re-evaluated before the dwell elapsed
        }
        catch (Exception ex)
        {
            // the entry stays - the exit can still report the dwell
            logger.LogWarning(ex, "Failed to check dwell for geofence {Identifier}", region.Identifier);
            this.Cancel(region.Identifier, cts);
        }
    }


    // only = remove the timer just if it is still this one (a timer finishing must not cancel its replacement)
    void Cancel(string identifier, CancellationTokenSource? only = null)
    {
        CancellationTokenSource? cts;
        bool removed;
        if (only == null)
        {
            removed = this.timers.TryRemove(identifier, out cts);
        }
        else
        {
            cts = only;
            removed = this.timers.TryRemove(new(identifier, only));
        }

        if (removed)
        {
            cts!.Cancel();
            cts.Dispose();
        }
    }
}
