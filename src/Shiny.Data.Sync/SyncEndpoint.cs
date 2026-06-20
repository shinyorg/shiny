using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shiny.Data.Sync;


/// <summary>
/// Configuration for a single sync endpoint (one URL, one entity type). Serialization is
/// delegated to <see cref="Shiny.Json.Default"/> (<see cref="Shiny.ISerializer"/>) — apps add
/// types via the source-generated <c>[ShinyJsonContext]</c> attribute and the engine resolves
/// them through the shared serializer chain, AOT-safely.
/// </summary>
public class SyncEndpoint
{
    /// <summary>
    /// Stable key for this endpoint. Defaults to the CLR full name of the entity type.
    /// Persisted with each <see cref="SyncOperation"/>, so changing this strands existing queued ops.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// The CLR type of the entity being sync'd.
    /// </summary>
    public required Type EntityType { get; init; }

    /// <summary>
    /// The base URL for the endpoint, e.g. <c>https://api.example.com/todos</c>. Used for
    /// outbox Create (POST), Update (PUT /{id}), Delete (DELETE /{id}), and — unless
    /// <see cref="PullUrl"/>/<see cref="BatchUrl"/> are set — inbox pulls and batched pushes.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Captured typed serializer for this endpoint's entity (set by the registry at
    /// <c>RegisterEndpoint&lt;T&gt;</c> time so the call site stays AOT-clean).
    /// </summary>
    internal Func<object, string> SerializeEntity { get; set; } = null!;

    /// <summary>
    /// Captured typed deserializer for this endpoint's entity (set by the registry at
    /// <c>RegisterEndpoint&lt;T&gt;</c> time so the call site stays AOT-clean).
    /// </summary>
    internal Func<string, object?> DeserializeEntity { get; set; } = null!;

    /// <summary>
    /// Which directions are supported. <see cref="SyncDirection.PullOnly"/> rejects calls to
    /// <see cref="IDataSyncManager.Queue{T}"/>; <see cref="SyncDirection.PushOnly"/> causes
    /// <see cref="IDataSyncManager.PullAll"/> to skip the endpoint silently.
    /// </summary>
    public SyncDirection Direction { get; set; } = SyncDirection.Both;

    /// <summary>
    /// When false, the engine waits for a non-metered connection before sending outbox ops.
    /// </summary>
    public bool UseMeteredConnection { get; set; } = true;

    /// <summary>
    /// When true, the outbox coalesces ops for this endpoint into a single batch request.
    /// </summary>
    public bool Batch { get; set; }

    /// <summary>
    /// Optional async hook invoked just before each request is sent. Runs after any global
    /// <see cref="ISyncInterceptor"/>s, so per-endpoint logic wins on header conflicts. On
    /// iOS / Mac Catalyst the request has no body attached (uploads stream from disk), so
    /// signers that hash the body won't work on Apple platforms.
    /// </summary>
    public Func<HttpRequestMessage, Task>? OnBeforeSend { get; set; }

    /// <summary>
    /// Default policy for conflicts on this endpoint.
    /// </summary>
    public ConflictPolicy DefaultConflictPolicy { get; set; } = ConflictPolicy.AskDelegate;

    /// <summary>
    /// Maximum number of send attempts before an op is permanently dropped to the delegate
    /// via <see cref="IDataSyncDelegate.OnError"/>. Default 5.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Base delay used for exponential backoff between retries (default 2s; the engine
    /// computes <c>baseDelay * 2^(attempts-1)</c>, capped at 60s).
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Query-string parameter used to send the persisted cursor to the server on inbox pulls.
    /// Default <c>since</c>; set to null to omit the cursor query parameter entirely.
    /// </summary>
    public string? CursorParameter { get; set; } = "since";

    /// <summary>
    /// Controls how scheduled (<see cref="IDataSyncManager.PullAll"/> / SyncJob / connectivity)
    /// pulls behave:
    /// <list type="bullet">
    /// <item><c>null</c> — <b>manual only</b>: automatic pulls skip this endpoint entirely; it pulls
    /// only when you call <see cref="IDataSyncManager.PullNow{T}"/>.</item>
    /// <item><see cref="TimeSpan.Zero"/> — <b>always pull</b> on every scheduled pass (the default
    /// applied at registration).</item>
    /// <item>A positive value — throttle: skip when the persisted
    /// <see cref="SyncCursor.LastPulledAt"/> is within this window.</item>
    /// </list>
    /// <see cref="IDataSyncManager.PullNow{T}"/> always bypasses this and pulls regardless.
    /// </summary>
    public TimeSpan? MinPullInterval { get; set; }

    /// <summary>
    /// Optional override for the inbox-pull URL. Defaults to <see cref="Url"/>.
    /// </summary>
    public string? PullUrl { get; set; }

    /// <summary>
    /// Optional override for the batched outbox URL. Defaults to <c>{Url}/batch</c>.
    /// </summary>
    public string? BatchUrl { get; set; }

    /// <summary>
    /// Optional URL for fetching server-side deletes. The endpoint should return a JSON array
    /// of entity IDs (<c>["id1","id2",...]</c>). When set, every inbox pull is followed by a
    /// tombstone fetch that dispatches <see cref="SyncVerb.Delete"/> records to delegates.
    /// </summary>
    public string? TombstoneUrl { get; set; }

    /// <summary>
    /// Query-string parameter used to send the persisted tombstone cursor. Default <c>since</c>;
    /// set to null to omit. Cursor is tracked independently from <see cref="SyncCursor"/>.
    /// </summary>
    public string? TombstoneCursorParameter { get; set; } = "since";

    /// <summary>
    /// Optional predicate evaluated on every deserialized inbox item. When it returns true
    /// the engine flips the verb from Create/Update to <see cref="SyncVerb.Delete"/> before
    /// dispatch — useful for servers that signal deletes via a flag (e.g. <c>IsDeleted</c>)
    /// rather than a separate event. The argument is the typed entity (cast as <see cref="object"/>).
    /// </summary>
    public Func<object, bool>? SoftDeletePredicate { get; set; }

    /// <summary>
    /// Optional predicate evaluated on every deserialized inbox item — same semantics as
    /// <see cref="SoftDeletePredicate"/> but intended for server-driven state changes
    /// (e.g. <c>AssignedTo == null</c>) where the client should evict the entity even though
    /// the server didn't mark it deleted.
    /// </summary>
    public Func<object, bool>? ExpiryPredicate { get; set; }
}


/// <summary>
/// Directional capability for a <see cref="SyncEndpoint"/>.
/// </summary>
public enum SyncDirection
{
    /// <summary>Both outbox (push) and inbox (pull) are allowed (default).</summary>
    Both,
    /// <summary>Only inbox pulls are supported — calls to <c>Queue</c> throw.</summary>
    PullOnly,
    /// <summary>Only outbox pushes are supported — <c>PullAll</c> skips, <c>PullNow</c> throws.</summary>
    PushOnly
}


/// <summary>
/// Default policy applied to conflicts before <see cref="IDataSyncDelegate.OnConflict"/> is invoked.
/// </summary>
public enum ConflictPolicy
{
    /// <summary>Always invoke the delegate to decide.</summary>
    AskDelegate,
    /// <summary>Always discard the local op and accept the remote state.</summary>
    ServerWins,
    /// <summary>Always retry the local op as-is.</summary>
    ClientWins
}


/// <summary>
/// Registration-time builder used inside <c>AddDataSync</c>.
/// </summary>
public interface ISyncEndpointBuilder
{
    /// <summary>
    /// Registers an endpoint for the given entity type. The entity's JSON shape must be
    /// reachable from a <c>[ShinyJsonContext]</c>-attributed <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>
    /// in the app — the engine resolves serialization through <see cref="Shiny.Json.Default"/>.
    /// </summary>
    void RegisterEndpoint<T>(string url, Action<SyncEndpoint>? configure = null) where T : ISyncEntity;
}
