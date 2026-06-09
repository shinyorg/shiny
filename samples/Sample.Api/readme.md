# Sample.Api

Minimal ASP.NET Core API that exposes the server side of two Shiny client libraries:

- **`Shiny.Net.Http`** large file transfers — raw + multipart uploads, range-aware downloads.
- **`Shiny.Data.Sync`** delta sync — CRUD, paginated cursor pull, and batched outbox endpoints in the exact wire shapes the default `RestSyncTransport` expects.

Persistence is `Shiny.DocumentDb.Sqlite` (`app.db` is created next to the binary on first run). Uploaded files land in `uploads/{guid}` on disk; metadata is in the doc store.

## Run

```bash
dotnet run --project samples/Sample.Api
# listens on http://localhost:5095
```

`GET /` returns a JSON map of every endpoint.

## File transfer endpoints

| Method | Route | Notes |
|---|---|---|
| `POST` | `/files/upload` | multipart/form-data (field name `file`) **or** raw body (`?name={fileName}` or `X-File-Name` header) |
| `GET`  | `/files` | list `FileRecord`s |
| `GET`  | `/files/{id}` | streams the file, supports `Range:` headers |
| `GET`  | `/files/{id}/info` | metadata only |
| `DELETE` | `/files/{id}` | removes the blob + record |

```bash
# Raw upload (matches Shiny.Net.Http TransferType.Upload)
curl -X POST 'http://localhost:5095/files/upload?name=video.mp4' \
     -H 'Content-Type: video/mp4' \
     --data-binary @./video.mp4

# Multipart upload (matches TransferType.UploadMultipart)
curl -X POST http://localhost:5095/files/upload \
     -F 'file=@./video.mp4'

# Resumable download
curl -H 'Range: bytes=1048576-' http://localhost:5095/files/{id} -o partial.bin
```

Kestrel's body size limit is disabled and `FormOptions.MultipartBodyLengthLimit` is `long.MaxValue`, so the only practical ceiling is disk space.

## Data sync endpoints

`Shiny.Data.Sync` configures a single endpoint URL per entity type. Point it at `http://localhost:5095/todos`:

```csharp
opts.RegisterEndpoint<TodoItem>("http://localhost:5095/todos");
```

Wire contract:

| Method | Route | Verb in `Shiny.Data.Sync` |
|---|---|---|
| `POST`   | `/todos` | `SyncVerb.Create` |
| `PUT`    | `/todos/{id}` | `SyncVerb.Update` |
| `DELETE` | `/todos/{id}` | `SyncVerb.Delete` |
| `GET`    | `/todos?since={cursor}&excludeDeletes={bool}` | inbox pull (`PullNow<TodoItem>` / `SyncJob`) |
| `GET`    | `/todos/tombstones?since={cursor}` | tombstone fetch — wire it on the client via `endpoint.TombstoneUrl` |
| `POST`   | `/todos/batch` | batched outbox (when `endpoint.Batch = true`) |
| `GET`    | `/todos/current` | convenience — current undeleted state (not part of the sync protocol) |

`GET /todos` returns the canonical pull shape:

```json
{
  "cursor": "2026-06-08T17:42:09.1234567+00:00",
  "hasMore": false,
  "items": [
    { "id": "abc", "verb": "Create", "payload": { "identifier": "abc", "title": "Hello", "completed": false, "updatedAt": "..." } },
    { "id": "abc", "verb": "Delete", "payload": null }
  ]
}
```

The cursor is the timestamp of the last row served; the client persists it via `SyncCursor` and sends it back on the next pull. Tombstone deletes are first-class entries with `payload: null`.

`POST /todos/batch` accepts the envelope `RestSyncTransport.SendBatch` produces and returns the `{ "results": [...] }` shape `BatchResultParser` expects.

## Storage layout

- `app.db` — SQLite document store. Three tables: `todos`, `sync_changes`, `files`.
- `uploads/{id}` — raw bytes for each uploaded file; the matching `FileRecord` in the `files` table carries metadata and the download URL.

Delete both to reset state.
