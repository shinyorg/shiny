using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Data.Sync;


/// <summary>
/// Manages background JSON sync operations that continue while the app is suspended.
/// </summary>
public interface IDataSyncManager
{
    /// <summary>
    /// Queues a sync operation for the given entity.
    /// </summary>
    /// <typeparam name="T">The synced entity type. Must be a registered endpoint.</typeparam>
    /// <param name="verb">The action to apply.</param>
    /// <param name="entity">The entity to sync.</param>
    /// <returns>The created operation in its initial Pending state.</returns>
    Task<SyncOperation> Queue<T>(SyncVerb verb, T entity) where T : ISyncEntity;

    /// <summary>
    /// Forces an immediate pull of server changes for the given entity type.
    /// </summary>
    Task PullNow<T>(CancellationToken cancelToken = default) where T : ISyncEntity;

    /// <summary>
    /// Forces an immediate pull of server changes for all registered endpoints.
    /// </summary>
    Task PullAll(CancellationToken cancelToken = default);

    /// <summary>
    /// Gets all queued operations currently in the outbox.
    /// </summary>
    Task<IList<SyncOperation>> GetPending();

    /// <summary>
    /// Cancels and removes a single queued operation.
    /// </summary>
    Task Cancel(string operationId);

    /// <summary>
    /// Cancels and removes all queued operations.
    /// </summary>
    Task CancelAll();

    /// <summary>
    /// Cancels and removes all queued operations targeting the given entity type.
    /// </summary>
    Task CancelAll<T>() where T : ISyncEntity;

    /// <summary>
    /// Raised when the outbox count changes.
    /// </summary>
    event EventHandler<int> PendingCountChanged;

    /// <summary>
    /// Raised on every state transition of a sync operation.
    /// </summary>
    event EventHandler<SyncOperationResult> UpdateReceived;

    /// <summary>
    /// Raised once per endpoint when an inbox pull completes (with or without items).
    /// </summary>
    event EventHandler<SyncPullCompletion> PullCompleted;

    /// <summary>
    /// Unified event stream covering every outbox + inbox lifecycle moment. Prefer this for
    /// app-wide telemetry / logging; the typed events above are kept for back-compat and
    /// fine-grained subscribers.
    /// </summary>
    event EventHandler<SyncEvent> Activity;
}


/// <summary>
/// Emitted from <see cref="IDataSyncManager.PullCompleted"/> on each finished inbox pull.
/// </summary>
/// <param name="EndpointKey">The endpoint key that was pulled.</param>
/// <param name="ItemsReceived">Number of deltas dispatched to <see cref="IDataSyncDelegate.OnReceived"/>.</param>
/// <param name="Cursor">The cursor recorded for the next pull (or the previous cursor if the server returned none).</param>
/// <param name="Error">The exception that aborted the pull, if any.</param>
public record SyncPullCompletion(
    string EndpointKey,
    int ItemsReceived,
    string? Cursor,
    Exception? Error
);
