using System;
using System.Threading.Tasks;

namespace Shiny.Data.Sync;


/// <summary>
/// Delegate the host app implements to receive sync lifecycle events.
/// </summary>
public interface IDataSyncDelegate
{
    /// <summary>
    /// Called when an outbox operation has been successfully transmitted to the server.
    /// </summary>
    /// <param name="operation">The completed operation.</param>
    /// <param name="responseBody">The raw response body returned by the server, if any.</param>
    Task OnSent(SyncOperation operation, string? responseBody);

    /// <summary>
    /// Called when an outbox operation fails after the transport has stopped retrying.
    /// </summary>
    /// <param name="operation">The failed operation.</param>
    /// <param name="statusCode">The HTTP status code, or 0 for non-HTTP errors.</param>
    /// <param name="ex">The exception that occurred.</param>
    Task OnError(SyncOperation operation, int statusCode, Exception ex);

    /// <summary>
    /// Called once per delta returned from an inbox pull. The engine deserializes the payload
    /// via <see cref="Shiny.Json.Default"/> (the shared <see cref="Shiny.ISerializer"/> from
    /// <c>Shiny.Extensions.Serialization</c>); the raw payload is also exposed for custom handling.
    /// </summary>
    /// <param name="item">The decoded delta.</param>
    Task OnReceived(SyncReceivedItem item);

    /// <summary>
    /// Called when the server rejects an outbox operation due to a conflict (HTTP 409 / 412).
    /// The delegate decides how to resolve.
    /// </summary>
    /// <param name="operation">The local operation that conflicted.</param>
    /// <param name="remotePayload">The current remote state, as returned by the server.</param>
    /// <returns>The resolution to apply.</returns>
    Task<ConflictResolution> OnConflict(SyncOperation operation, string remotePayload);
}


/// <summary>
/// A delta delivered to <see cref="IDataSyncDelegate.OnReceived"/>.
/// </summary>
/// <param name="EndpointKey">The endpoint key the entity belongs to.</param>
/// <param name="EntityType">The registered CLR type for the endpoint.</param>
/// <param name="Entity">The deserialized entity, or null if deserialization failed.</param>
/// <param name="RawPayload">The raw JSON payload as returned by the server.</param>
/// <param name="Verb">The verb the server reported (Create/Update/Delete).</param>
public record SyncReceivedItem(
    string EndpointKey,
    Type EntityType,
    object? Entity,
    string RawPayload,
    SyncVerb Verb
);


/// <summary>
/// The action a conflict resolution should take.
/// </summary>
public enum ConflictAction
{
    /// <summary>Discard the local operation and accept the remote state.</summary>
    AcceptRemote,
    /// <summary>Force the local operation through; the server should accept it.</summary>
    KeepLocal,
    /// <summary>Replace the local operation's payload with a merged result and retry.</summary>
    UseMerged
}


/// <summary>
/// Resolution returned by <see cref="IDataSyncDelegate.OnConflict"/>.
/// </summary>
public record ConflictResolution(ConflictAction Action, string? MergedPayload = null)
{
    /// <summary>Discard the local operation and accept the remote state.</summary>
    public static ConflictResolution AcceptRemote { get; } = new(ConflictAction.AcceptRemote);

    /// <summary>Force the local operation through; the server should accept it.</summary>
    public static ConflictResolution KeepLocal { get; } = new(ConflictAction.KeepLocal);

    /// <summary>Replace the local operation's payload with a merged result and retry.</summary>
    public static ConflictResolution UseMerged(string mergedPayload) => new(ConflictAction.UseMerged, mergedPayload);
}
