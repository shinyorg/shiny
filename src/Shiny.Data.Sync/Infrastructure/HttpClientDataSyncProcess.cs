using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
    IEnumerable<IDataSyncDelegate> delegates
)
{
    static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(60);

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
                var ops = this.GetReadyPending();
                while (!cancelSrc.IsCancellationRequested && ops.Count > 0)
                {
                    if (connectivity.IsInternetAvailable())
                    {
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
                            if (cancelSrc.IsCancellationRequested)
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
                                await this.RunBatch(endpoint, list, coalesced, cancelSrc.Token).ConfigureAwait(false);
                            }
                            else
                            {
                                var live = list.Where(op => repository.Exists<SyncOperation>(op.Identifier));
                                foreach (var op in live)
                                {
                                    if (cancelSrc.IsCancellationRequested)
                                        break;
                                    await this.RunOperation(endpoint, op, cancelSrc.Token).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    else
                    {
                        logger.LogDebug("Internet unavailable - waiting for next pass");
                    }

                    ops = this.GetReadyPending();
                    if (ops.Count > 0)
                        await Task.Delay(10000, cancelSrc.Token).ConfigureAwait(false);
                }
                logger.LogDebug("Outbox drained");
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in sync outbox loop");
            }
            onComplete();
        });
    }


    /// <summary>
    /// Returns pending ops in deterministic creation order, skipping any whose backoff window
    /// has not yet elapsed (avoids hot-spinning on a flapping server).
    /// </summary>
    IList<SyncOperation> GetReadyPending()
    {
        var now = DateTimeOffset.UtcNow;
        var ready = repository
            .GetAll<SyncOperation>()
            .Where(op => !op.NextAttemptAt.HasValue || op.NextAttemptAt.Value <= now)
            .ToList();
        ready.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
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
            foreach (var d in delegates)
            {
                try { await d.OnSent(inProgress, result.ResponseBody).ConfigureAwait(false); }
                catch (Exception dex) { logger.LogError(dex, "Sync delegate OnSent threw"); }
            }
            repository.Remove(inProgress);
            Publish(inProgress, SyncOperationState.Completed, result.StatusCode, null);
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


    async Task HandleError(SyncEndpoint endpoint, SyncOperation op, int statusCode, Exception ex)
    {
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

        foreach (var d in delegates)
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
        var delayMs = (long)endpoint.RetryBaseDelay.TotalMilliseconds * (long)Math.Pow(2, Math.Max(0, attempts - 1));
        var delay = TimeSpan.FromMilliseconds(Math.Min(delayMs, MaxBackoff.TotalMilliseconds));
        var nextAt = DateTimeOffset.UtcNow + delay;

        logger.StandardWarn(op.Identifier,
            $"{(reason ?? "Transient error")} (status {statusCode}, attempts {attempts}/{endpoint.MaxAttempts}) - retry in {delay.TotalSeconds:N1}s: {ex.Message}");

        if (!repository.Exists<SyncOperation>(op.Identifier))
            return;

        var pending = op with { State = SyncOperationState.Pending, LastError = ex.Message, NextAttemptAt = nextAt };
        repository.Set(pending);
        Publish(pending, SyncOperationState.Pending, statusCode == 0 ? null : statusCode, ex);
    }


    async Task RunBatch(SyncEndpoint endpoint, IList<SyncOperation> originals, IReadOnlyList<SyncOperation> coalesced, CancellationToken cancelToken)
    {
        logger.LogInformation("Sending batch of {count} coalesced ops ({original} originals) for endpoint {key}",
            coalesced.Count, originals.Count, endpoint.Key);

        foreach (var op in coalesced)
        {
            if (repository.Exists<SyncOperation>(op.Identifier))
                repository.Set(op with { State = SyncOperationState.InProgress, Attempts = op.Attempts + 1, NextAttemptAt = null });
        }

        SyncBatchResult result;
        try
        {
            result = await transport.SendBatch(endpoint, coalesced, cancelToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Batch send failed for endpoint {key}", endpoint.Key);
            var live = originals.Where(op => repository.Exists<SyncOperation>(op.Identifier));
            foreach (var op in live)
            {
                var current = repository.Get<SyncOperation>(op.Identifier)!;
                this.ScheduleRetry(endpoint, current, 0, ex, "Batch send failed");
            }
            return;
        }

        var coalescedById = new Dictionary<string, SyncOperation>();
        foreach (var op in coalesced)
            coalescedById[op.Identifier] = op;

        foreach (var item in result.Items)
        {
            if (!coalescedById.TryGetValue(item.OperationIdentifier, out var coalescedOp))
            {
                // result references an op we didn't send - drop it silently
            }
            else if (item.IsConflict)
            {
                await this.HandleConflict(endpoint, coalescedOp, item.StatusCode, item.ResponseBody ?? string.Empty).ConfigureAwait(false);
            }
            else if (item.StatusCode < 200 || item.StatusCode > 299)
            {
                var ex = new InvalidOperationException(item.ErrorMessage ?? $"Batch item failed with status {item.StatusCode}");
                await this.HandleError(endpoint, coalescedOp, item.StatusCode, ex).ConfigureAwait(false);
            }
            else
            {
                foreach (var d in delegates)
                {
                    try { await d.OnSent(coalescedOp, item.ResponseBody).ConfigureAwait(false); }
                    catch (Exception dex) { logger.LogError(dex, "Sync delegate OnSent threw"); }
                }
                Publish(coalescedOp, SyncOperationState.Completed, item.StatusCode, null);
            }
        }

        var coalescedEntityIds = new HashSet<string>();
        foreach (var op in coalesced)
            coalescedEntityIds.Add(op.EntityIdentifier);

        foreach (var original in originals)
        {
            if (coalescedEntityIds.Contains(original.EntityIdentifier) && repository.Exists<SyncOperation>(original.Identifier))
                repository.Remove(original);
        }
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

                await DispatchReceived(endpoint, remotePayload).ConfigureAwait(false);
                Publish(op, SyncOperationState.Completed, statusCode, null);
                break;

            case ConflictAction.KeepLocal:
                // Re-queue the op as Pending; the next outbox pass will re-send it as-is.
                // Servers that want a "force overwrite" header should set it via OnBeforeSend.
                logger.StandardInfo(op.Identifier, "Conflict resolved: keep local - retrying");
                if (repository.Exists<SyncOperation>(op.Identifier))
                    repository.Set(op with { State = SyncOperationState.Pending, NextAttemptAt = null });
                Publish(op, SyncOperationState.Pending, statusCode, null);
                break;

            case ConflictAction.UseMerged:
                logger.StandardInfo(op.Identifier, "Conflict resolved: merged payload - retrying");
                if (repository.Exists<SyncOperation>(op.Identifier))
                {
                    var merged = op with { Payload = resolution.MergedPayload, State = SyncOperationState.Pending, NextAttemptAt = null };
                    repository.Set(merged);
                    Publish(merged, SyncOperationState.Pending, statusCode, null);
                }
                break;
        }
    }


    async Task DispatchReceived(SyncEndpoint endpoint, string remotePayload)
    {
        object? entity = null;
        try { entity = SyncInboxProcessor.DeserializeEntity(endpoint, remotePayload); }
        catch (Exception ex) { logger.LogError(ex, "Failed to deserialize conflict remote payload for {key}", endpoint.Key); }

        var item = new SyncReceivedItem(endpoint.Key, endpoint.EntityType, entity, remotePayload, SyncVerb.Update);
        foreach (var d in delegates)
        {
            try { await d.OnReceived(item).ConfigureAwait(false); }
            catch (Exception dex) { logger.LogError(dex, "Delegate OnReceived threw during conflict resolution"); }
        }
    }


    async Task<ConflictResolution> AskDelegate(SyncOperation op, string remotePayload)
    {
        foreach (var d in delegates)
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
