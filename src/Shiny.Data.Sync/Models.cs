using System;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Data.Sync;


/// <summary>
/// The action being applied to an entity in the sync stream.
/// </summary>
public enum SyncVerb
{
    /// <summary>The entity is being created on the server.</summary>
    Create,
    /// <summary>An existing entity is being updated.</summary>
    Update,
    /// <summary>An existing entity is being deleted.</summary>
    Delete
}


/// <summary>
/// Lifecycle state of a queued sync operation.
/// </summary>
public enum SyncOperationState
{
    /// <summary>The operation is queued and waiting to run.</summary>
    Pending,
    /// <summary>The operation is currently being transmitted.</summary>
    InProgress,
    /// <summary>The operation completed successfully.</summary>
    Completed,
    /// <summary>The operation failed and will be retried.</summary>
    Error,
    /// <summary>The operation was canceled before completion.</summary>
    Canceled,
    /// <summary>The server reported a conflict that requires delegate resolution.</summary>
    ConflictPending
}


/// <summary>
/// A single queued sync operation persisted in the outbox.
/// </summary>
/// <param name="Identifier">Globally unique id for this operation.</param>
/// <param name="EndpointKey">The key of the <see cref="SyncEndpoint"/> this operation targets.</param>
/// <param name="EntityIdentifier">The identifier of the entity being sync'd.</param>
/// <param name="Verb">The action being applied.</param>
/// <param name="Payload">The serialized JSON body for Create/Update; null for Delete.</param>
/// <param name="State">Current lifecycle state.</param>
/// <param name="CreatedAt">When the operation was queued.</param>
/// <param name="Attempts">Number of send attempts made so far.</param>
/// <param name="LastError">The error message from the most recent failed attempt, if any.</param>
/// <param name="NextAttemptAt">When the outbox loop is allowed to retry this op; null = ready now.</param>
public record SyncOperation(
    string Identifier,
    string EndpointKey,
    string EntityIdentifier,
    SyncVerb Verb,
    string? Payload,
    SyncOperationState State,
    DateTimeOffset CreatedAt,
    int Attempts,
    string? LastError,
    DateTimeOffset? NextAttemptAt = null
) : IRepositoryEntity;


/// <summary>
/// A snapshot emitted on every state transition of a <see cref="SyncOperation"/>.
/// </summary>
/// <param name="Operation">The operation the update relates to.</param>
/// <param name="State">The new state.</param>
/// <param name="StatusCode">The HTTP status code, when applicable.</param>
/// <param name="Exception">The exception, when the operation is in an error state.</param>
public record SyncOperationResult(
    SyncOperation Operation,
    SyncOperationState State,
    int? StatusCode,
    Exception? Exception
);
