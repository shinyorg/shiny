using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Data.Sync.Infrastructure;


/// <summary>
/// The wire-level transport used by the outbox/inbox processors.
/// Default implementation is <see cref="RestSyncTransport"/>.
/// </summary>
public interface ISyncTransport
{
    /// <summary>
    /// Sends a single outbox operation to the server.
    /// </summary>
    Task<SyncSendResult> Send(SyncEndpoint endpoint, SyncOperation operation, CancellationToken cancelToken);

    /// <summary>
    /// Pulls server-side changes for the endpoint since the supplied cursor.
    /// </summary>
    Task<SyncPullResult> Pull(SyncEndpoint endpoint, string? cursor, CancellationToken cancelToken);

    /// <summary>
    /// Sends a batch of coalesced operations for the endpoint in a single request.
    /// </summary>
    Task<SyncBatchResult> SendBatch(SyncEndpoint endpoint, IReadOnlyList<SyncOperation> ops, CancellationToken cancelToken);

    /// <summary>
    /// Fetches the list of entity IDs the server has deleted since the supplied cursor.
    /// Only called when <see cref="SyncEndpoint.TombstoneUrl"/> is non-null.
    /// </summary>
    Task<SyncTombstoneResult> FetchTombstones(SyncEndpoint endpoint, string? cursor, CancellationToken cancelToken);
}


/// <summary>
/// Outcome of a single batched send. Per-op results are positional, matching the input order.
/// </summary>
public record SyncBatchResult(
    int StatusCode,
    IReadOnlyList<SyncBatchItemResult> Items
);


/// <summary>
/// Per-operation outcome inside a <see cref="SyncBatchResult"/>.
/// </summary>
public record SyncBatchItemResult(
    string OperationIdentifier,
    int StatusCode,
    string? ResponseBody,
    bool IsConflict,
    string? ErrorMessage
);


/// <summary>
/// Outcome of a single outbox send.
/// </summary>
public record SyncSendResult(
    int StatusCode,
    string? ResponseBody,
    bool IsConflict
);


/// <summary>
/// Outcome of a single inbox pull. <c>HasMore</c> tells the engine to immediately
/// re-pull with the new cursor — used for server-side pagination of large delta sets.
/// </summary>
public record SyncPullResult(
    string? NextCursor,
    IReadOnlyList<SyncPullItem> Items,
    bool HasMore = false,
    bool NotModified = false
);


/// <summary>
/// A single item returned from a delta pull.
/// </summary>
public record SyncPullItem(
    string EntityIdentifier,
    SyncVerb Verb,
    string Payload
);


/// <summary>
/// Outcome of a single tombstone fetch. <c>DeletedIdentifiers</c> is the set of entity IDs the
/// server reports as deleted; the engine dispatches each as a <see cref="SyncVerb.Delete"/> to
/// <see cref="IDataSyncDelegate.OnReceived"/>.
/// </summary>
public record SyncTombstoneResult(
    string? NextCursor,
    IReadOnlyList<string> DeletedIdentifiers,
    bool NotModified = false
);
