using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Data.Sync.Infrastructure;


/// <summary>
/// Pulls delta updates from each registered endpoint and dispatches them to the delegate.
/// Runs in-process — invoked by <see cref="IDataSyncManager.PullNow{T}"/>, by app lifecycle hooks,
/// or by the scheduled <c>SyncJob</c>. Loops while the server reports <see cref="SyncPullResult.HasMore"/>
/// so a single call drains the full delta set, then optionally runs a tombstone fetch.
/// </summary>
public class SyncInboxProcessor(
    ILogger<SyncInboxProcessor> logger,
    IRepository repository,
    ISyncTransport transport,
    SyncEndpointRegistry registry,
    IEnumerable<IDataSyncDelegate> delegates
)
{
    /// <summary>
    /// Raised once per endpoint when a pull (including paginated rounds + tombstones) finishes.
    /// Mirrors <see cref="IDataSyncManager.PullCompleted"/> for non-Apple platforms.
    /// </summary>
    public event EventHandler<SyncPullCompletion>? PullCompleted;


    /// <summary>
    /// Fine-grained activity stream — start, per-item, completion, failure, tombstone application.
    /// </summary>
    public event EventHandler<SyncEvent>? Activity;


    public Task PullEndpoint(SyncEndpoint endpoint, CancellationToken cancelToken = default)
        => this.PullEndpoint(endpoint, force: true, cancelToken);


    public async Task PullEndpoint(SyncEndpoint endpoint, bool force, CancellationToken cancelToken = default)
    {
        if (endpoint.Direction == SyncDirection.PushOnly)
            return;

        if (!force && IsThrottled(repository, endpoint, out var skippedReason))
        {
            logger.LogDebug("Skipping pull for {key}: {reason}", endpoint.Key, skippedReason);
            return;
        }

        var cursor = repository.Get<SyncCursor>(endpoint.Key)?.Cursor;
        var startingCursor = cursor;
        var totalDispatched = 0;
        Exception? error = null;

        this.RaiseActivity(new SyncEvent(SyncEventType.InboxPullStarted, endpoint.Key, Cursor: cursor));

        try
        {
            while (true)
            {
                cancelToken.ThrowIfCancellationRequested();

                SyncPullResult result;
                try
                {
                    result = await transport.Pull(endpoint, cursor, cancelToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Pull failed for endpoint {key}", endpoint.Key);
                    error = ex;
                    break;
                }

                if (result.NotModified)
                {
                    logger.LogDebug("Pull for {key} returned 304 - no changes", endpoint.Key);
                    break;
                }

                foreach (var item in result.Items)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    var dispatched = await this.DispatchItem(endpoint, item).ConfigureAwait(false);
                    if (dispatched)
                        totalDispatched++;
                }

                cursor = result.NextCursor ?? cursor;
                if (!result.HasMore)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // caller canceled - fall through to record final state
        }

        var finalCursor = cursor ?? startingCursor;
        repository.Set(new SyncCursor(endpoint.Key, finalCursor, DateTimeOffset.UtcNow));

        // Tombstones only run on a successful main pull and only when the endpoint asks for them.
        if (error == null && !string.IsNullOrEmpty(endpoint.TombstoneUrl))
        {
            try { await this.RunTombstones(endpoint, cancelToken).ConfigureAwait(false); }
            catch (Exception ex) { logger.LogError(ex, "Tombstone fetch failed for {key}", endpoint.Key); }
        }

        this.PullCompleted?.Invoke(this, new SyncPullCompletion(endpoint.Key, totalDispatched, finalCursor, error));
        this.RaiseActivity(new SyncEvent(
            error != null ? SyncEventType.InboxPullFailed : SyncEventType.InboxPullCompleted,
            endpoint.Key,
            ItemCount: totalDispatched,
            Cursor: finalCursor,
            Error: error
        ));
    }


    public async Task PullAll(CancellationToken cancelToken = default)
    {
        foreach (var endpoint in registry.All)
        {
            if (cancelToken.IsCancellationRequested)
                return;
            await this.PullEndpoint(endpoint, force: false, cancelToken).ConfigureAwait(false);
        }
    }


    async Task<bool> DispatchItem(SyncEndpoint endpoint, SyncPullItem item)
    {
        object? entity = null;
        try { entity = DeserializeEntity(endpoint, item.Payload); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to deserialize inbox payload for {key}, entity {id}", endpoint.Key, item.EntityIdentifier);
        }

        var verb = item.Verb;
        if (entity != null && verb != SyncVerb.Delete)
        {
            if (endpoint.SoftDeletePredicate?.Invoke(entity) == true || endpoint.ExpiryPredicate?.Invoke(entity) == true)
                verb = SyncVerb.Delete;
        }

        var received = new SyncReceivedItem(endpoint.Key, endpoint.EntityType, entity, item.Payload, verb);

        foreach (var d in delegates)
        {
            try
            {
                await d.OnReceived(received).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Delegate OnReceived threw for endpoint {key}, entity {id}", endpoint.Key, item.EntityIdentifier);
            }
        }

        this.RaiseActivity(new SyncEvent(SyncEventType.InboxItemReceived, endpoint.Key, Item: received));
        return true;
    }


    async Task RunTombstones(SyncEndpoint endpoint, CancellationToken cancelToken)
    {
        var tombCursor = repository.Get<SyncTombstoneCursor>(endpoint.Key)?.Cursor;
        var result = await transport.FetchTombstones(endpoint, tombCursor, cancelToken).ConfigureAwait(false);

        if (result.NotModified)
            return;

        foreach (var id in result.DeletedIdentifiers)
        {
            cancelToken.ThrowIfCancellationRequested();
            await this.DispatchTombstone(endpoint, id).ConfigureAwait(false);
        }

        var nextCursor = result.NextCursor ?? tombCursor;
        repository.Set(new SyncTombstoneCursor(endpoint.Key, nextCursor, DateTimeOffset.UtcNow));

        if (result.DeletedIdentifiers.Count > 0)
        {
            this.RaiseActivity(new SyncEvent(
                SyncEventType.TombstonesApplied,
                endpoint.Key,
                ItemCount: result.DeletedIdentifiers.Count,
                Cursor: nextCursor
            ));
        }
    }


    async Task DispatchTombstone(SyncEndpoint endpoint, string entityId)
    {
        var received = new SyncReceivedItem(endpoint.Key, endpoint.EntityType, Entity: null, RawPayload: "null", SyncVerb.Delete);
        foreach (var d in delegates)
        {
            try { await d.OnReceived(received).ConfigureAwait(false); }
            catch (Exception ex) { logger.LogError(ex, "Delegate OnReceived threw for tombstone {key}/{id}", endpoint.Key, entityId); }
        }
        this.RaiseActivity(new SyncEvent(SyncEventType.InboxItemReceived, endpoint.Key, Item: received));
    }


    static bool IsThrottled(IRepository repository, SyncEndpoint endpoint, out string reason)
    {
        reason = string.Empty;
        if (!endpoint.MinPullInterval.HasValue)
            return false;

        var last = repository.Get<SyncCursor>(endpoint.Key)?.LastPulledAt;
        if (!last.HasValue)
            return false;

        var elapsed = DateTimeOffset.UtcNow - last.Value;
        if (elapsed < endpoint.MinPullInterval.Value)
        {
            reason = $"throttled - {elapsed.TotalSeconds:N0}s since last pull, min {endpoint.MinPullInterval.Value.TotalSeconds:N0}s";
            return true;
        }
        return false;
    }


    void RaiseActivity(SyncEvent evt)
        => this.Activity?.Invoke(this, evt);


    internal static object? DeserializeEntity(SyncEndpoint endpoint, string payload)
        => endpoint.DeserializeEntity(payload);
}
