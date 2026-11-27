using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores.Repositories;
using Shiny.Net;

namespace Shiny.Data.Sync.Infrastructure;


/// <summary>
/// Drains the outbox via the configured <see cref="ISyncTransport"/> while the process is alive.
/// Used directly on Windows/Linux/Blazor, and hosted inside the Android foreground service.
/// </summary>
public class HttpClientDataSyncProcess(
    ILogger<HttpClientDataSyncProcess> logger,
    IRepository repository,
    IConnectivity connectivity,
    ISyncTransport transport,
    SyncEndpointRegistry registry,
    IServiceProvider services
)
{
    static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(60);
    static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
    static readonly TimeSpan MinPassDelay = TimeSpan.FromMilliseconds(100);

    public static event EventHandler<SyncOperationResult>? ProgressOccurred;


    public void Run(Action onComplete)
    {
        _ = Task.Run(async () =>
        {
            logger.LogInformation("Starting Sync Outbox Loop");
            using var cancelSrc = new CancellationTokenSource();

            EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> clearHandler = null!;
            clearHandler = (_, x) =>
            {
                if (x.EntityType == typeof(SyncOperation) && x.Action == RepositoryAction.Clear)
                {
                    repository.ActionOccurred -= clearHandler;
                    logger.LogInformation("Sync outbox cleared - cancelling loop");
                    cancelSrc.Cancel();
                }
            };
            repository.ActionOccurred += clearHandler;
            using var sub = new RepoSub(() => repository.ActionOccurred -= clearHandler);

            try
            {
                // Keep looping while anything is queued - including ops waiting out a retry backoff,
                // which GetReadyPending leaves out. Exiting on "nothing ready" stranded those ops until
                // the next Queue/Start/connectivity change.
                var hasWork = repository.GetAll<SyncOperation>().Count > 0;
                while (!cancelSrc.IsCancellationRequested && hasWork)
                {
                    try
                    {
                        await this.RunPass(cancelSrc.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancelSrc.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // a failed pass must not end the loop - the queued ops would wait for the next start
                        logger.LogError(ex, "Error in sync outbox pass");
                    }

                    hasWork = repository.GetAll<SyncOperation>().Count > 0;
                    if (hasWork)
                        await Task.Delay(this.GetNextPassDelay(), cancelSrc.Token).ConfigureAwait(false);
                }
                logger.LogDebug("Outbox drained");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in sync outbox loop");
            }
            onComplete();
        });
    }


    async Task RunPass(CancellationToken cancelToken)
    {
        var ops = this.GetReadyPending();
        if (ops.Count == 0)
            return;

        if (!connectivity.IsInternetAvailable())
        {
            logger.LogDebug("Internet unavailable - waiting for next pass");
            return;
        }

        var unmetered = connectivity.ConnectionTypes.HasFlag(ConnectionTypes.Wifi);
        var groups = new Dictionary<string, List<SyncOperation>>();
        foreach (var op in ops)
        {
            if (!groups.TryGetValue(op.EndpointKey, out var list))
            {
                list = new List<SyncOperation>();
                groups[op.EndpointKey] = list;
            }
            list.Add(op);
        }

        foreach (var (key, list) in groups)
        {
            if (cancelToken.IsCancellationRequested)
                break;

            var endpoint = registry.Get(key);
            if (endpoint == null)
            {
                logger.StandardWarn(key, $"No endpoint registered for key '{key}' - dropping {list.Count} ops");
                var orphans = list.Where(op => repository.Exists<SyncOperation>(op.Identifier));
                foreach (var op in orphans)
                    repository.Remove(op);
            }
            else if (!endpoint.UseMeteredConnection && !unmetered)
            {
                logger.StandardInfo(key, "Waiting for unmetered connection");
            }
            else if (endpoint.Batch && list.Count > 1)
            {
                var coalesced = OperationCoalescer.Coalesce(list);
                await this.RunBatch(endpoint, list, coalesced, cancelToken).ConfigureAwait(false);
            }
            else
            {
                // Once an op for an entity is still queued after its attempt (retry scheduled, conflict kept
                // local), later ops for that entity wait - sending them now would reach the server out of order.
                var blockedEntities = new HashSet<string>();
                var live = list.Where(op =>
                    !blockedEntities.Contains(op.EntityIdentifier) &&
                    repository.Exists<SyncOperation>(op.Identifier)
                );
                foreach (var op in live)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    await this.RunOperation(endpoint, op, cancelToken).ConfigureAwait(false);
                    if (repository.Exists<SyncOperation>(op.Identifier))
                        blockedEntities.Add(op.EntityIdentifier);
                }
            }
        }
    }


    /// <summary>
    /// Time until the next pass: the default poll interval, shortened to the earliest retry backoff.
    /// </summary>
    TimeSpan GetNextPassDelay()
    {
        var now = DateTimeOffset.UtcNow;
        var delay = PollInterval;
        var waiting = repository
            .GetAll<SyncOperation>()
            .Where(op => op.NextAttemptAt.HasValue && op.NextAttemptAt.Value > now);

        foreach (var op in waiting)
        {
            var untilReady = op.NextAttemptAt!.Value - now;
            if (untilReady < delay)
                delay = untilReady;
        }
        return delay < MinPassDelay ? MinPassDelay : delay;
    }


    /// <summary>
    /// Returns pending ops in deterministic creation order, skipping any whose backoff window
    /// has not yet elapsed (avoids hot-spinning on a flapping server).
    /// </summary>
    IList<SyncOperation> GetReadyPending()
    {
        var now = DateTimeOffset.UtcNow;
        var all = repository.GetAll<SyncOperation>().ToList();
        all.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));

        // an op still in its backoff window holds back every later op for the same entity,
        // so the server sees them in the order they were queued
        var blockedEntities = new HashSet<string>();
        var ready = new List<SyncOperation>();
        foreach (var op in all)
        {
            var isReady = !op.NextAttemptAt.HasValue || op.NextAttemptAt.Value <= now;
            if (!isReady)
                blockedEntities.Add(op.EntityIdentifier);
            else if (!blockedEntities.Contains(op.EntityIdentifier))
                ready.Add(op);
        }
        return ready;
    }


    async Task RunOperation(SyncEndpoint endpoint, SyncOperation op, CancellationToken cancelToken)
    {
        using var cancelSrc = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);

        EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> removeHandler = null!;
        removeHandler = (_, x) =>
        {
            if (x.EntityType == typeof(SyncOperation) && x.Action == RepositoryAction.Remove && op.Identifier.Equals(x.Entity!.Identifier))
            {
                repository.ActionOccurred -= removeHandler;
                cancelSrc.Cancel();
            }
        };
        repository.ActionOccurred += removeHandler;
        using var sub = new RepoSub(() => repository.ActionOccurred -= removeHandler);

        var inProgress = op with { State = SyncOperationState.InProgress, Attempts = op.Attempts + 1, NextAttemptAt = null };
        repository.Set(inProgress);
        Publish(inProgress, SyncOperationState.InProgress, null, null);

        try
        {
            var result = await transport.Send(endpoint, inProgress, cancelSrc.Token).ConfigureAwait(false);

            if (result.IsConflict)
            {
                await this.HandleConflict(endpoint, inProgress, result.StatusCode, result.ResponseBody ?? string.Empty).ConfigureAwait(false);
                return;
            }

            logger.StandardInfo(op.Identifier, "Sent successfully");
            await this.Complete(inProgress, result.StatusCode, result.ResponseBody).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // op removed or loop canceled
        }
        catch (HttpRequestException ex)
        {
            await this.HandleError(endpoint, inProgress, (int?)ex.StatusCode ?? 0, ex).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            this.HandleTransient(endpoint, inProgress, "Network IO error", ex);
        }
#if ANDROID
        catch (Java.Net.SocketException ex)
        {
            this.HandleTransient(endpoint, inProgress, "Network disconnected", ex);
        }
#endif
        catch (Exception ex)
        {
            await this.HandleError(endpoint, inProgress, 0, ex).ConfigureAwait(false);
        }
    }


    /// <summary>
    /// The op reached the server (or, for a DELETE answered 404 / 410, the entity is already gone): run
    /// <see cref="IDataSyncDelegate.OnSent"/>, drop it from the outbox and report it completed.
    /// </summary>
    async Task Complete(SyncOperation op, int statusCode, string? responseBody)
    {
        foreach (var d in services.GetServices<IDataSyncDelegate>())
        {
            try { await d.OnSent(op, responseBody).ConfigureAwait(false); }
            catch (Exception dex) { logger.LogError(dex, "Sync delegate OnSent threw"); }
        }
        if (repository.Exists<SyncOperation>(op.Identifier))
            repository.Remove(op);

        Publish(op, SyncOperationState.Completed, statusCode, null);
    }


    async Task HandleError(SyncEndpoint endpoint, SyncOperation op, int statusCode, Exception ex)
    {
        if (endpoint.IsAlreadyDeleted(op.Verb, statusCode))
        {
            logger.StandardInfo(op.Identifier, $"DELETE returned {statusCode} - entity already gone, completing");
            await this.Complete(op, statusCode, null).ConfigureAwait(false);
            return;
        }

        var isTransient = statusCode == 0 || statusCode >= 500 || statusCode == 408 || statusCode == 429;
        if (isTransient && op.Attempts < endpoint.MaxAttempts)
        {
            this.ScheduleRetry(endpoint, op, statusCode, ex);
            return;
        }

        logger.LogError(ex, "Sync operation {id} failed permanently (status {code}, attempts {attempts})", op.Identifier, statusCode, op.Attempts);

        var updated = op with { State = SyncOperationState.Error, LastError = ex.Message, NextAttemptAt = null };
        if (repository.Exists<SyncOperation>(op.Identifier))
            repository.Set(updated);

        foreach (var d in services.GetServices<IDataSyncDelegate>())
        {
            try { await d.OnError(updated, statusCode, ex).ConfigureAwait(false); }
            catch (Exception dex) { logger.LogError(dex, "Sync delegate OnError threw"); }
        }

        if (repository.Exists<SyncOperation>(op.Identifier))
            repository.Remove(updated);

        Publish(updated, SyncOperationState.Error, statusCode, ex);
    }


    void HandleTransient(SyncEndpoint endpoint, SyncOperation op, string reason, Exception ex)
    {
        if (op.Attempts >= endpoint.MaxAttempts)
        {
            _ = this.HandleError(endpoint, op, 0, ex);
            return;
        }
        this.ScheduleRetry(endpoint, op, 0, ex, reason);
    }


    void ScheduleRetry(SyncEndpoint endpoint, SyncOperation op, int statusCode, Exception ex, string? reason = null)
    {
        var attempts = op.Attempts;
        var delay = GetRetryDelay(endpoint, op);
        var nextAt = DateTimeOffset.UtcNow + delay;

        logger.StandardWarn(op.Identifier,
            $"{(reason ?? "Transient error")} (status {statusCode}, attempts {attempts}/{endpoint.MaxAttempts}) - retry in {delay.TotalSeconds:N1}s: {ex.Message}");

        if (!repository.Exists<SyncOperation>(op.Identifier))
            return;

        var pending = op with { State = SyncOperationState.Pending, LastError = ex.Message, NextAttemptAt = nextAt };
        repository.Set(pending);
        Publish(pending, SyncOperationState.Pending, statusCode == 0 ? null : statusCode, ex);
    }


    static TimeSpan GetRetryDelay(SyncEndpoint endpoint, SyncOperation op)
    {
        var delayMs = (long)endpoint.RetryBaseDelay.TotalMilliseconds * (long)Math.Pow(2, Math.Max(0, op.Attempts - 1));
        return TimeSpan.FromMilliseconds(Math.Min(delayMs, MaxBackoff.TotalMilliseconds));
    }


    async Task RunBatch(SyncEndpoint endpoint, IList<SyncOperation> originals, IReadOnlyList<SyncOperation> coalesced, CancellationToken cancelToken)
    {
        var coalescedIdByEntity = new Dictionary<string, string>();
        foreach (var op in coalesced)
            coalescedIdByEntity[op.EntityIdentifier] = op.Identifier;

        // A Create that never reached the server followed by its Delete coalesces to nothing: remove both and
        // report them canceled - removed from the outbox without being sent, the same as Cancel(). No delegate
        // runs: OnSent would claim a server round trip that never happened, and nothing failed.
        var dropped = originals
            .Where(op =>
                !coalescedIdByEntity.ContainsKey(op.EntityIdentifier) &&
                repository.Exists<SyncOperation>(op.Identifier)
            )
            .ToList();

        foreach (var op in dropped)
        {
            logger.StandardInfo(op.Identifier, "Create was never sent and is now deleted - dropping without sending");
            repository.Remove(op);
            Publish(op, SyncOperationState.Canceled, null, null);
        }

        if (coalesced.Count == 0)
            return;

        logger.LogInformation("Sending batch of {count} coalesced ops ({original} originals) for endpoint {key}",
            coalesced.Count, originals.Count, endpoint.Key);

        // Record the attempt on the originals too: a batch that throws (timeout) may still have reached the server,
        // so a Create in it must not look unsent to the coalescer afterwards.
        var liveOriginals = originals.Where(op => repository.Exists<SyncOperation>(op.Identifier));
        foreach (var op in liveOriginals)
            repository.Set(op with { State = SyncOperationState.InProgress, Attempts = op.Attempts + 1, NextAttemptAt = null });

        // what goes out carries the attempt it is on, so retry limits apply to batched ops too
        var sending = coalesced
            .Select(op => op with { State = SyncOperationState.InProgress, Attempts = op.Attempts + 1, NextAttemptAt = null })
            .ToList();

        SyncBatchResult result;
        try
        {
            result = await transport.SendBatch(endpoint, sending, cancelToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Batch send failed for endpoint {key}", endpoint.Key);
            var live = originals.Where(op => repository.Exists<SyncOperation>(op.Identifier));
            foreach (var op in live)
            {
                var current = repository.Get<SyncOperation>(op.Identifier)!;
                this.HandleTransient(endpoint, current, "Batch send failed", ex);
            }
            return;
        }

        var sentById = new Dictionary<string, SyncOperation>();
        foreach (var op in sending)
            sentById[op.Identifier] = op;

        foreach (var item in result.Items)
        {
            if (!sentById.TryGetValue(item.OperationIdentifier, out var sentOp))
            {
                // result references an op we didn't send - drop it silently
            }
            else if (item.IsConflict)
            {
                await this.HandleConflict(endpoint, sentOp, item.StatusCode, item.ResponseBody ?? string.Empty).ConfigureAwait(false);
            }
            else if (item.StatusCode < 200 || item.StatusCode > 299)
            {
                var ex = new InvalidOperationException(item.ErrorMessage ?? $"Batch item failed with status {item.StatusCode}");
                await this.HandleError(endpoint, sentOp, item.StatusCode, ex).ConfigureAwait(false);
            }
            else
            {
                await this.Complete(sentOp, item.StatusCode, item.ResponseBody).ConfigureAwait(false);
            }
        }

        // Every original folded into a coalesced op is now represented by it: drop them. The coalesced op
        // itself (stored under the first original's id) is left alone - it is either gone (sent, failed
        // permanently, remote accepted) or was re-queued with the coalesced payload for a retry.
        var folded = originals.Where(original =>
            coalescedIdByEntity.TryGetValue(original.EntityIdentifier, out var coalescedId) &&
            !coalescedId.Equals(original.Identifier) &&
            repository.Exists<SyncOperation>(original.Identifier)
        );
        foreach (var original in folded.ToList())
            repository.Remove(original);
    }


    async Task HandleConflict(SyncEndpoint endpoint, SyncOperation op, int statusCode, string remotePayload)
    {
        Publish(op, SyncOperationState.ConflictPending, statusCode, null);

        var resolution = endpoint.DefaultConflictPolicy switch
        {
            ConflictPolicy.ServerWins => ConflictResolution.AcceptRemote,
            ConflictPolicy.ClientWins => ConflictResolution.KeepLocal,
            _ => await this.AskDelegate(op, remotePayload).ConfigureAwait(false)
        };

        switch (resolution.Action)
        {
            case ConflictAction.AcceptRemote:
                logger.StandardInfo(op.Identifier, "Conflict resolved: accept remote - dropping op");
                if (repository.Exists<SyncOperation>(op.Identifier))
                    repository.Remove(op);

                await DispatchReceived(endpoint, op.EntityIdentifier, remotePayload).ConfigureAwait(false);
                Publish(op, SyncOperationState.Completed, statusCode, null);
                break;

            case ConflictAction.KeepLocal:
            case ConflictAction.UseMerged:
                // Re-queue the op as Pending; the next outbox pass re-sends it (with the merged payload if
                // there is one). Servers that want a "force overwrite" header should set it via OnBeforeSend.
                var retry = resolution.Action == ConflictAction.UseMerged
                    ? op with { Payload = resolution.MergedPayload }
                    : op;

                if (retry.Attempts >= endpoint.MaxAttempts)
                {
                    // the server keeps rejecting it - fail instead of re-sending it every pass forever
                    var ex = new InvalidOperationException($"Conflict (HTTP {statusCode}) still unresolved after {retry.Attempts} attempts");
                    await this.HandleError(endpoint, retry, statusCode, ex).ConfigureAwait(false);
                }
                else if (repository.Exists<SyncOperation>(op.Identifier))
                {
                    logger.StandardInfo(op.Identifier, $"Conflict resolved: {resolution.Action} - retrying");
                    var pending = retry with
                    {
                        State = SyncOperationState.Pending,
                        NextAttemptAt = DateTimeOffset.UtcNow + GetRetryDelay(endpoint, retry)
                    };
                    repository.Set(pending);
                    Publish(pending, SyncOperationState.Pending, statusCode, null);
                }
                break;
        }
    }


    async Task DispatchReceived(SyncEndpoint endpoint, string entityIdentifier, string remotePayload)
    {
        object? entity = null;
        try { entity = SyncInboxProcessor.DeserializeEntity(endpoint, remotePayload); }
        catch (Exception ex) { logger.LogError(ex, "Failed to deserialize conflict remote payload for {key}", endpoint.Key); }

        var item = new SyncReceivedItem(endpoint.Key, endpoint.EntityType, entity, remotePayload, SyncVerb.Update, entityIdentifier);
        foreach (var d in services.GetServices<IDataSyncDelegate>())
        {
            try { await d.OnReceived(item).ConfigureAwait(false); }
            catch (Exception dex) { logger.LogError(dex, "Delegate OnReceived threw during conflict resolution"); }
        }
    }


    async Task<ConflictResolution> AskDelegate(SyncOperation op, string remotePayload)
    {
        foreach (var d in services.GetServices<IDataSyncDelegate>())
        {
            try
            {
                return await d.OnConflict(op, remotePayload).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Delegate OnConflict threw - falling back to AcceptRemote");
            }
        }
        return ConflictResolution.AcceptRemote;
    }


    static void Publish(SyncOperation op, SyncOperationState state, int? statusCode, Exception? ex)
        => ProgressOccurred?.Invoke(null, new SyncOperationResult(op, state, statusCode, ex));
}
