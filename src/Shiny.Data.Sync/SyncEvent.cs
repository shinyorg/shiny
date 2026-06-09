using System;

namespace Shiny.Data.Sync;


/// <summary>
/// Lifecycle moment emitted via <see cref="IDataSyncManager.Activity"/>.
/// </summary>
public enum SyncEventType
{
    /// <summary>An outbox op was queued via <c>Queue&lt;T&gt;</c>.</summary>
    OutboxQueued,
    /// <summary>An outbox op started sending (NSURLSession upload kicked off, or HttpClient send begun).</summary>
    OutboxStarted,
    /// <summary>An outbox op succeeded and was removed from the queue.</summary>
    OutboxSent,
    /// <summary>An outbox op failed permanently after exhausting its retry budget.</summary>
    OutboxFailed,
    /// <summary>An outbox op got an HTTP 409 / 412 from the server.</summary>
    OutboxConflict,
    /// <summary>An outbox op hit a transient failure and was requeued with backoff.</summary>
    OutboxRetryScheduled,
    /// <summary>An outbox op was canceled before completion.</summary>
    OutboxCanceled,
    /// <summary>An inbox pull started for the endpoint.</summary>
    InboxPullStarted,
    /// <summary>A single inbox item was dispatched to delegates.</summary>
    InboxItemReceived,
    /// <summary>An inbox pull finished successfully (with or without items).</summary>
    InboxPullCompleted,
    /// <summary>An inbox pull failed (network error, parse error, non-2xx status).</summary>
    InboxPullFailed,
    /// <summary>A tombstone fetch returned one or more deleted IDs and dispatched them as Delete records.</summary>
    TombstonesApplied
}


/// <summary>
/// Single event emitted from the unified <see cref="IDataSyncManager.Activity"/> stream.
/// Only the fields relevant to <see cref="Type"/> are populated.
/// </summary>
/// <param name="Type">Which moment in the sync lifecycle fired this event.</param>
/// <param name="EndpointKey">The endpoint key the event relates to.</param>
/// <param name="Operation">The outbox operation, when <see cref="Type"/> is an outbox event.</param>
/// <param name="Item">The dispatched inbox item, for <see cref="SyncEventType.InboxItemReceived"/>.</param>
/// <param name="StatusCode">HTTP status code, when applicable.</param>
/// <param name="ItemCount">Number of items dispatched (for InboxPullCompleted / TombstonesApplied).</param>
/// <param name="Cursor">The cursor recorded after a pull, when applicable.</param>
/// <param name="Error">The exception, when the event represents a failure.</param>
public record SyncEvent(
    SyncEventType Type,
    string EndpointKey,
    SyncOperation? Operation = null,
    SyncReceivedItem? Item = null,
    int? StatusCode = null,
    int? ItemCount = null,
    string? Cursor = null,
    Exception? Error = null
);
