using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Data.Sync.Infrastructure;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Data.Sync;


/// <summary>
/// Android implementation. The outbox loop runs inside a foreground service
/// (<see cref="DataSyncService"/>) so the OS keeps the process alive until the queue drains.
/// </summary>
public class DataSyncManager(
    AndroidPlatform platform,
    SyncInboxProcessor inbox,
    ILogger<DataSyncManager> logger,
    IRepository repository,
    SyncEndpointRegistry registry
) : IDataSyncManager, IShinyStartupTask, IDisposable
{
    bool subscribed;

    public void Start()
    {
        if (!this.subscribed)
        {
            HttpClientDataSyncProcess.ProgressOccurred += this.OnProcessProgress;
            inbox.PullCompleted += this.OnInboxCompleted;
            inbox.Activity += this.OnInboxActivity;
            repository.ActionOccurred += this.OnRepoAction;
            this.subscribed = true;
        }

        try
        {
            if (DataSyncService.IsStarted)
                return;

            var ops = repository.GetAll<SyncOperation>();
            if (ops.Count > 0)
                this.TryStartService();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to auto-start Data Sync Manager");
        }
    }


    public void Dispose()
    {
        if (this.subscribed)
        {
            HttpClientDataSyncProcess.ProgressOccurred -= this.OnProcessProgress;
            inbox.PullCompleted -= this.OnInboxCompleted;
            inbox.Activity -= this.OnInboxActivity;
            repository.ActionOccurred -= this.OnRepoAction;
            this.subscribed = false;
        }
    }


    void OnProcessProgress(object? sender, SyncOperationResult result)
    {
        this.UpdateReceived?.Invoke(this, result);

        var eventType = result.State switch
        {
            SyncOperationState.InProgress => SyncEventType.OutboxStarted,
            SyncOperationState.Completed => SyncEventType.OutboxSent,
            SyncOperationState.Error => SyncEventType.OutboxFailed,
            SyncOperationState.ConflictPending => SyncEventType.OutboxConflict,
            SyncOperationState.Canceled => SyncEventType.OutboxCanceled,
            SyncOperationState.Pending when result.StatusCode != null || result.Exception != null
                => SyncEventType.OutboxRetryScheduled,
            _ => (SyncEventType?)null
        };
        if (eventType is { } et)
            this.Activity?.Invoke(this, new SyncEvent(et, result.Operation.EndpointKey, Operation: result.Operation, StatusCode: result.StatusCode, Error: result.Exception));
    }


    void OnInboxCompleted(object? sender, SyncPullCompletion completion)
        => this.PullCompleted?.Invoke(this, completion);


    void OnInboxActivity(object? sender, SyncEvent evt)
        => this.Activity?.Invoke(this, evt);


    void OnRepoAction(object? sender, (RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity) x)
    {
        if (x.EntityType != typeof(SyncOperation) || x.Action == RepositoryAction.Update)
            return;
        this.PendingCountChanged?.Invoke(this, repository.GetAll<SyncOperation>().Count);
    }


    public Task<IList<SyncOperation>> GetPending()
    {
        var list = repository.GetAll<SyncOperation>().ToList();
        return Task.FromResult<IList<SyncOperation>>(list);
    }


    public Task<SyncOperation> Queue<T>(SyncVerb verb, T entity) where T : ISyncEntity
    {
        var op = HttpClientDataSyncManager.BuildOperation(registry, verb, entity);
        repository.Insert(op);
        this.Activity?.Invoke(this, new SyncEvent(SyncEventType.OutboxQueued, op.EndpointKey, Operation: op));
        this.TryStartService();
        return Task.FromResult(op);
    }


    public Task PullNow<T>(CancellationToken cancelToken = default) where T : ISyncEntity
    {
        var endpoint = registry.Get<T>()
            ?? throw new InvalidOperationException($"No sync endpoint registered for type {typeof(T).FullName}");
        if (endpoint.Direction == SyncDirection.PushOnly)
            throw new InvalidOperationException($"Endpoint '{endpoint.Key}' is configured PushOnly - PullNow is not allowed.");
        return inbox.PullEndpoint(endpoint, force: true, cancelToken);
    }


    public Task PullAll(CancellationToken cancelToken = default) => inbox.PullAll(cancelToken);


    public Task Cancel(string operationId)
    {
        var op = repository.Get<SyncOperation>(operationId);
        if (op != null)
        {
            repository.Remove(op);
            this.UpdateReceived?.Invoke(this, new SyncOperationResult(op, SyncOperationState.Canceled, null, null));
            this.Activity?.Invoke(this, new SyncEvent(SyncEventType.OutboxCanceled, op.EndpointKey, Operation: op));
        }
        return Task.CompletedTask;
    }


    public Task CancelAll()
    {
        repository.Clear<SyncOperation>();
        return Task.CompletedTask;
    }


    public Task CancelAll<T>() where T : ISyncEntity
    {
        var key = typeof(T).FullName!;
        var toRemove = repository.GetAll<SyncOperation>().Where(x => x.EndpointKey == key).ToList();
        foreach (var op in toRemove)
            repository.Remove(op);
        return Task.CompletedTask;
    }


    public event EventHandler<int>? PendingCountChanged;
    public event EventHandler<SyncOperationResult>? UpdateReceived;
    public event EventHandler<SyncPullCompletion>? PullCompleted;
    public event EventHandler<SyncEvent>? Activity;


    void TryStartService()
    {
        if (!DataSyncService.IsStarted)
            platform.StartService(typeof(DataSyncService), true);
    }
}
