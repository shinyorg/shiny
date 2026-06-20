---
name: shiny-data-sync
description: Guide for generating code that uses Shiny.Data.Sync for reliable, background-capable bidirectional JSON sync over HTTP on iOS, Android, Windows, Linux, macOS, and Blazor WASM
auto_invoke: true
triggers:
  - data sync
  - background sync
  - outbox
  - inbox
  - delta sync
  - DataSyncManager
  - IDataSyncManager
  - IDataSyncDelegate
  - SyncOperation
  - SyncEndpoint
  - ISyncEntity
  - Shiny.Data.Sync
  - offline-first
  - eventual consistency
  - conflict resolution
  - PullNow
---

# Shiny Data Sync

Reliable, background-capable bidirectional JSON sync between a mobile/desktop app and an HTTP backend. Mirrors the platform-tier guarantees of Shiny.Net.Http (NSURLSession on Apple, Foreground Service on Android, HttpClient fallback elsewhere), but for **structured records** rather than files.

Provides an **outbox** for queued local changes (Create/Update/Delete with JSON payloads), an **inbox** for delta pulls from the server, retry with exponential backoff, pluggable conflict resolution, per-endpoint batching, and AOT-compatible JSON via `JsonTypeInfo`.

## When to Use This Skill

Use this skill when the user needs to:

- Sync app entities to a REST backend reliably, even when offline or backgrounded
- Build an offline-first feature that eventually consistent-syncs to the server
- Queue Create/Update/Delete operations that must survive app kill / device reboot
- Pull server-side changes on a schedule (delta sync)
- Resolve sync conflicts (HTTP 409 / 412) with custom merge logic
- Batch multiple queued operations into a single server round-trip
- Persist a per-endpoint cursor for delta pulls

Do **not** use this skill for: large file uploads/downloads (use `shiny-http-transfers`), realtime data streams (use SignalR/MQTT), or push-driven sync (use `shiny-push` to trigger `PullNow`).

## Library Overview

| Item        | Value                                                                                   |
|-------------|-----------------------------------------------------------------------------------------|
| NuGet       | `Shiny.Data.Sync`, `Shiny.Data.Sync.Blazor`                                             |
| Namespace   | `Shiny.Data.Sync`                                                                       |
| Platforms   | iOS / Mac Catalyst (NSURLSession background, both outbox upload + inbox download); Android (Foreground Service + HttpClient); Windows / Linux / base .NET (HttpClient + connectivity loop); Blazor WASM (HttpClient + LocalStorage) |
| DI Setup    | `services.AddDataSync<TDelegate>(builder => ...)` on all native platforms (auto-picks NSURLSession / Foreground Service / HttpClient based on TFM), `services.AddBlazorDataSync<TDelegate>(...)` on Blazor WASM |

## Setup

### 1. Define an Entity

Implement `ISyncEntity` on any record you want to sync:

```csharp
using Shiny.Data.Sync;

public record TodoItem(string Identifier, string Title, bool Completed) : ISyncEntity;
```

### 2. Register Services

In `MauiProgram.cs` (reflection-friendly path):

```csharp
using Shiny;

builder.Services.AddDataSync<MyDataSyncDelegate>(opts =>
{
    opts.RegisterEndpoint<TodoItem>("https://api.example.com/todos");

    opts.RegisterEndpoint<Project>("https://api.example.com/projects", ep =>
    {
        // -- Direction --
        ep.Direction = SyncDirection.Both;           // or PullOnly / PushOnly

        // -- Network policy --
        ep.UseMeteredConnection = false;             // wait for WiFi
        ep.Batch = true;                             // coalesce ops per round-trip
        ep.MaxAttempts = 8;                          // retry transient failures up to 8x
        ep.RetryBaseDelay = TimeSpan.FromSeconds(3);

        // -- Conflicts --
        ep.DefaultConflictPolicy = ConflictPolicy.ServerWins;

        // -- Inbox throttle / scheduling --
        // null        = manual only: SyncJob/PullAll skip this endpoint; only PullNow<T> pulls it
        // TimeSpan.Zero = always pull on every scheduled pass (the default applied at registration)
        // positive     = throttle: SyncJob/PullAll skip while within the window; PullNow bypasses
        ep.MinPullInterval = TimeSpan.FromMinutes(5);

        // -- Per-verb URL overrides (optional) --
        ep.PullUrl = "https://api.example.com/projects/feed";  // GET different URL on pull
        ep.BatchUrl = "https://api.example.com/projects/bulk"; // POST batched ops elsewhere
        ep.CursorParameter = "updatedSince";          // default "since"

        // -- Tombstones (server-side delete stream) --
        ep.TombstoneUrl = "https://api.example.com/projects/deleted";
        ep.TombstoneCursorParameter = "since";

        // -- Soft-delete / expiry predicates --
        ep.SoftDeletePredicate = entity => entity is Project p && p.IsArchived;
        ep.ExpiryPredicate     = entity => entity is Project p && p.OwnerId == null;

        // -- Per-endpoint request hook (after ISyncInterceptor) --
        ep.OnBeforeSend = req =>
        {
            req.Headers.Add("X-Trace-Id", Guid.NewGuid().ToString("N"));
            return Task.CompletedTask;
        };
    });
});

// Global auth — runs before every endpoint's OnBeforeSend
builder.Services.AddSyncInterceptor<MyAuthInterceptor>();

// Centralize base address / Polly handlers on the named client
builder.Services.AddHttpClient(RestSyncTransport.HttpClientName, c =>
    c.BaseAddress = new Uri("https://api.example.com")
);
```

#### Native AOT / trimmed builds

All serialization runs through `Shiny.Json.Default` (the shared `ISerializer` from `Shiny.Extensions.Serialization`). Add the entity type to a context decorated with `[ShinyJsonContext]` — a source-generated module initializer wires it into the shared chain before any code runs:

```csharp
[ShinyJsonContext]
[JsonSerializable(typeof(TodoItem))]
[JsonSerializable(typeof(Project))]
public partial class AppJsonContext : JsonSerializerContext;
```

The same context covers every endpoint that uses these types — no per-endpoint plumbing. To customize options globally use `services.ConfigureJsonSerializer(opts => ...)` from `Shiny.Extensions.Serialization`.

### 3. Implement the Delegate

```csharp
using Shiny.Data.Sync;

public class MyDataSyncDelegate : IDataSyncDelegate
{
    public Task OnSent(SyncOperation op, string? responseBody)
    {
        // Server accepted the op. responseBody may contain a server-assigned id, new ETag, etc.
        return Task.CompletedTask;
    }

    public Task OnError(SyncOperation op, int statusCode, Exception ex)
    {
        // Op exhausted its retry budget. Persist to a dead-letter store or notify the user.
        return Task.CompletedTask;
    }

    public Task OnReceived(SyncReceivedItem item)
    {
        // The engine already deserialized item.Entity using the endpoint's JsonTypeInfo
        // or JsonOptions. RawPayload is also available for custom handling.
        if (item.Entity is TodoItem todo)
            myLocalStore.Apply(todo, item.Verb);
        return Task.CompletedTask;
    }

    public Task<ConflictResolution> OnConflict(SyncOperation op, string remotePayload)
    {
        // The server already has a newer version. Decide what to do.
        return Task.FromResult(ConflictResolution.AcceptRemote);
    }
}
```

## Common Tasks

### Queue an outbox operation

```csharp
public class TodosService(IDataSyncManager sync)
{
    public Task CreateTodo(TodoItem item) => sync.Queue(SyncVerb.Create, item);
    public Task UpdateTodo(TodoItem item) => sync.Queue(SyncVerb.Update, item);
    public Task DeleteTodo(TodoItem item) => sync.Queue(SyncVerb.Delete, item);
}
```

### Force a pull (pull-to-refresh)

```csharp
public Task RefreshTodos(IDataSyncManager sync, CancellationToken ct)
    => sync.PullNow<TodoItem>(ct);
```

### Observe outbox + inbox progress

```csharp
// Typed events for fine-grained subscribers
sync.PendingCountChanged += (s, count) => StatusLabel.Text = $"{count} pending";
sync.UpdateReceived += (s, result) =>
{
    if (result.State == SyncOperationState.Error)
        ShowToast($"Sync failed: {result.Exception?.Message}");
};
sync.PullCompleted += (s, c) =>
{
    if (c.Error != null) ShowToast($"Pull failed for {c.EndpointKey}: {c.Error.Message}");
    else if (c.ItemsReceived > 0) ShowToast($"{c.ItemsReceived} new {c.EndpointKey} items");
};

// Unified Activity stream — covers every lifecycle moment
sync.Activity += (s, evt) =>
{
    Console.WriteLine($"{evt.Type} {evt.EndpointKey} items={evt.ItemCount} status={evt.StatusCode}");
};
```

`Activity` fires `SyncEvent` records for: `OutboxQueued`, `OutboxStarted`, `OutboxSent`, `OutboxFailed`, `OutboxConflict`, `OutboxRetryScheduled`, `OutboxCanceled`, `InboxPullStarted`, `InboxItemReceived`, `InboxPullCompleted`, `InboxPullFailed`, `TombstonesApplied`.

### Cancel queued work

```csharp
await sync.Cancel(operationId);              // one operation
await sync.CancelAll<TodoItem>();            // everything for one endpoint
await sync.CancelAll();                       // entire outbox (in-flight inbox pulls are left alone)
```

## Platform Behavior

| Platform | Outbox transport | Inbox transport | Survives app kill? |
|---|---|---|---|
| iOS / Mac Catalyst | Background `NSURLSession` upload task | Background `NSURLSession` download task | **Yes** (both directions) |
| Android | Foreground Service + HttpClient | HttpClient (in-process) | Outbox **yes** (notification visible while syncing); inbox no |
| Windows / Linux / base .NET | HttpClient + connectivity loop | HttpClient + connectivity loop | No — resumes on next launch |
| Blazor WASM | HttpClient + LocalStorage | HttpClient + LocalStorage | No — syncs while tab is open |

On iOS the JSON payload is serialized to a temp file (background NSURLSessions require file-backed uploads); the temp file is cleaned up when the operation completes or fails. The same background `NSUrlSession` carries both upload and download tasks (`HttpMaximumConnectionsPerHost = 4`).

A regained network connection on the HttpClient platforms (Windows / Linux / desktop) automatically triggers an outbox drain and a full `PullAll` via the `IConnectivity.Changed` event.

## Retry policy

Every endpoint has `MaxAttempts` (default 5) and `RetryBaseDelay` (default 2s). Transient failures — `HttpStatusCode 0` (network down), `5xx`, `408`, `429` — schedule a retry at `baseDelay * 2^(attempts-1)` capped at 60s. The retry timestamp is persisted on the `SyncOperation` as `NextAttemptAt`, so a process restart resumes the wait window correctly. After `MaxAttempts`, the op is handed to `IDataSyncDelegate.OnError` and removed from the outbox (the delegate can re-queue if it wants).

## Conflict Resolution

When the server returns `409 Conflict` or `412 Precondition Failed`, the engine consults the endpoint's `DefaultConflictPolicy`:

- `AskDelegate` (default) — calls `IDataSyncDelegate.OnConflict`
- `ServerWins` — drops the local op, dispatches the remote payload through `OnReceived` as an Update
- `ClientWins` — re-queues the local op as-is

The delegate's `OnConflict` returns:

- `ConflictResolution.AcceptRemote` — same as `ServerWins`
- `ConflictResolution.KeepLocal` — re-queue the local op
- `ConflictResolution.UseMerged(string mergedPayload)` — replace the op's payload with a merge result and retry

## Tombstones (separate delete stream)

Some servers can't merge deletes into the main pull. Set `endpoint.TombstoneUrl` and the engine
follows every successful pull with a GET against that URL, expecting one of two shapes:

```json
["id1", "id2", ...]
```

or, when the server paginates / cursors deletes separately:

```json
{ "cursor": "<opaque next cursor>", "ids": ["id1","id2",...] }
```

Each ID dispatched to `IDataSyncDelegate.OnReceived` with `Verb = Delete` and `Entity = null`.
A separate `SyncTombstoneCursor` record persists the tombstone cursor independently from the
main `SyncCursor`. On iOS / Mac Catalyst the tombstone fetch also rides the background
NSURLSession (`tombstone:{endpointKey}` task description), so it survives suspension just
like the main pull.

## Soft-delete and Expiry predicates

When a server signals deletes via a flag on the entity (`IsDeleted = true`) or via a state
change that the client should treat as eviction (`AssignedTo = null`), point the engine at it:

```csharp
ep.SoftDeletePredicate = e => e is Project p && p.IsArchived;
ep.ExpiryPredicate     = e => e is Project p && p.OwnerId == null;
```

Both run on the deserialized entity inside the inbox dispatch loop, before delegates fire.
When either returns `true` for a Create/Update item, the verb is rewritten to `Delete` and
`Entity` stays populated (so consumers can read the final state on the way out the door).

## Direction (PullOnly / PushOnly / Both)

Set `ep.Direction` to restrict what's allowed:

- `Both` (default) — `Queue` + `PullNow` + `PullAll` all work
- `PullOnly` — `Queue` throws; the server is the source of truth
- `PushOnly` — `PullNow` throws, `PullAll` silently skips this endpoint; useful for telemetry / SyncUp queues

## ISyncInterceptor (global request hook)

Per-endpoint `OnBeforeSend` handles endpoint-specific tweaks, but cross-cutting auth is
better as a single `ISyncInterceptor`:

```csharp
public class AuthInterceptor(ITokenService tokens) : ISyncInterceptor
{
    public Task BeforePull(SyncEndpoint endpoint, string? cursor, HttpRequestMessage req)
    {
        req.Headers.Authorization = new("Bearer", tokens.Current());
        return Task.CompletedTask;
    }

    public Task BeforePush(SyncEndpoint endpoint, IReadOnlyList<SyncOperation> ops, HttpRequestMessage req)
    {
        req.Headers.Authorization = new("Bearer", tokens.Current());
        return Task.CompletedTask;
    }

    // BeforeTombstoneFetch's default forwards to BeforePull — override only if your
    // tombstone URL is on a different auth domain.
}

builder.Services.AddSyncInterceptor<AuthInterceptor>();
```

All registered interceptors run *before* the per-endpoint `OnBeforeSend`, so endpoint
hooks still win on header conflicts. Multiple interceptors are supported and execute in
registration order.

## Named HttpClient

The engine resolves its transport from `IHttpClientFactory` under the name
`RestSyncTransport.HttpClientName` (`"Shiny.Data.Sync"`). Use the named-client pattern to
attach base addresses, Polly handlers, and so on:

```csharp
builder.Services
    .AddHttpClient(RestSyncTransport.HttpClientName, c =>
    {
        c.BaseAddress = new Uri("https://api.example.com");
        c.Timeout = TimeSpan.FromMinutes(2);
    })
    .AddPolicyHandler(GetRetryPolicy());
```

## Inbox response shape

The default `RestSyncTransport` expects:

```json
{
  "cursor": "<opaque next cursor>",
  "hasMore": false,
  "items": [
    { "id": "<entity id>", "verb": "Create|Update|Delete", "payload": { ... } }
  ]
}
```

If `hasMore: true`, the engine immediately re-pulls with the new cursor — drains the full delta set in one `PullNow`/`PullAll` call. HTTP `304 Not Modified` is treated as "no changes" and just bumps `LastPulledAt` without touching the cursor. Apps that need a different shape can implement `ISyncTransport` directly.

## Batching

Set `endpoint.Batch = true` to coalesce multiple queued ops for one endpoint into a single `POST {url}/batch` request. Coalescing rules:

- Trailing `Delete` wins — any preceding Create/Update for the same entity drop
- `Create + Update(s)` → single `Create` with the latest payload
- `Update + Update(s)` → single `Update` with the latest payload

Batch response shape expected from the server:

```json
{
  "results": [
    { "id": "<op-id>", "status": 200, "body": { ... }, "error": null }
  ]
}
```

Batching applies to the HttpClient process path (Android Foreground Service, Windows, Linux, Blazor). The iOS path sends one NSURLSession upload task per op by design.

## Custom Transports

Override `ISyncTransport` to use a non-REST protocol (gRPC, GraphQL, custom envelopes):

```csharp
public class GrpcSyncTransport : ISyncTransport { /* ... */ }

builder.Services.AddDataSync<MyDelegate>(opts => { /* ... */ });
builder.Services.AddSingleton<ISyncTransport, GrpcSyncTransport>();   // overrides the default REST transport
```

## Caveats

- **Apple `OnBeforeSend` doesn't see the body.** Uploads stream from disk on iOS / Mac Catalyst, so the stub `HttpRequestMessage` passed to `OnBeforeSend` has headers only. Signers that hash the body (AWS SigV4, etc.) won't work on Apple — use server-side validation or the Android/desktop path.
- **Apple `PullAll` is fire-and-forget.** The returned `Task` completes once tasks are kicked off, not when the downloads finish — observe `PullCompleted` for completion.
- **Apple session shares the connection limit.** Uploads and downloads share `HttpMaximumConnectionsPerHost = 4`; a saturated outbox queues inbox pulls behind it.
- **Inbox in-flight requests are lost on process exit for HttpClient platforms.** Only iOS / Mac Catalyst gets the survives-app-kill guarantee for pulls.
- **Overlapping pulls are coalesced per endpoint.** If a pull for an entity type is already in progress, a second request for the same type is skipped (this applies even to `PullNow<T>`) — so a connectivity trigger, the `SyncJob`, and a manual refresh firing together won't race on the same cursor.
