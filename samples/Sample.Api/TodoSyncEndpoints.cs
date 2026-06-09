using System.Text.Json;
using Shiny.DocumentDb;

namespace Sample.Api;


public static class TodoSyncEndpoints
{
    const string EntityTypeName = "TodoItem";
    const int DefaultPageSize = 200;

    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/todos");

        // ---- Convenience: current state (not part of the Shiny.Data.Sync protocol) ----
        group.MapGet("/current", async (IDocumentStore store) =>
            await store.Query<TodoItem>().OrderBy(t => t.UpdatedAt).ToList());


        // ---- Shiny.Data.Sync inbox: delta pull with cursor ----
        // Set ?excludeDeletes=true to mirror the soft-delete pattern: deletes are returned
        // as Updates carrying `isDeleted: true` instead of a separate verb, and the client
        // configures SoftDeletePredicate to convert them on receipt.
        group.MapGet("/", async (string? since, int? limit, bool? excludeDeletes, IDocumentStore store) =>
        {
            var take = Math.Clamp(limit ?? DefaultPageSize, 1, 1000);
            var sinceTime = TryParseCursor(since);
            var skipDeleteVerb = excludeDeletes == true;

            var changes = await store.Query<SyncChange>()
                .Where(c => c.EntityType == EntityTypeName && c.Timestamp > sinceTime)
                .OrderBy(c => c.Timestamp)
                .Paginate(0, take + 1)
                .ToList();

            var hasMore = changes.Count > take;
            var page = hasMore ? changes.Take(take).ToList() : changes;

            var nextCursor = page.Count > 0
                ? page[^1].Timestamp.ToString("O")
                : since;

            var items = page
                .Where(c => !(skipDeleteVerb && c.Verb == "Delete"))
                .Select(c => new SyncPullItem(
                    Id: c.EntityId,
                    Verb: c.Verb,
                    Payload: ParsePayload(c.Payload)
                ))
                .ToList();

            return Results.Ok(new SyncPullResponse(nextCursor, hasMore, items));
        });


        // ---- Shiny.Data.Sync tombstones: separate stream of deleted ids ----
        group.MapGet("/tombstones", async (string? since, IDocumentStore store) =>
        {
            var sinceTime = TryParseCursor(since);

            var deletes = await store.Query<SyncChange>()
                .Where(c => c.EntityType == EntityTypeName && c.Verb == "Delete" && c.Timestamp > sinceTime)
                .OrderBy(c => c.Timestamp)
                .ToList();

            var nextCursor = deletes.Count > 0 ? deletes[^1].Timestamp.ToString("O") : since;
            var ids = deletes.Select(c => c.EntityId).Distinct().ToList();

            return Results.Ok(new { cursor = nextCursor, ids });
        });


        // ---- Shiny.Data.Sync outbox: per-op endpoints ----
        group.MapPost("/", async (TodoItem item, IDocumentStore store)
            => Results.Created($"/todos/{item.Identifier}", await CreateOrReplace(store, item)));

        group.MapPut("/{id}", async (string id, TodoItem item, IDocumentStore store) =>
        {
            item.Identifier = id;
            return Results.Ok(await CreateOrReplace(store, item, isUpdate: true));
        });

        group.MapDelete("/{id}", async (string id, IDocumentStore store) =>
        {
            var existed = await store.Remove<TodoItem>(id);
            if (!existed)
                return Results.NotFound();

            await AppendChange(store, id, "Delete", payload: null);
            return Results.NoContent();
        });


        // ---- Shiny.Data.Sync batched outbox ----
        group.MapPost("/batch", async (SyncBatchRequest req, IDocumentStore store) =>
        {
            var results = new List<SyncBatchResultItem>(req.Operations.Count);

            foreach (var op in req.Operations)
            {
                try
                {
                    JsonElement? body = null;
                    switch (op.Verb)
                    {
                        case "Create":
                        case "Update":
                            if (op.Payload is not JsonElement p)
                                throw new InvalidOperationException("Payload is required for Create/Update.");
                            var item = p.Deserialize<TodoItem>(WireJson)!;
                            item.Identifier = op.EntityId;
                            var stored = await CreateOrReplace(store, item, isUpdate: op.Verb == "Update");
                            body = SerializeToElement(stored);
                            break;

                        case "Delete":
                            await store.Remove<TodoItem>(op.EntityId);
                            await AppendChange(store, op.EntityId, "Delete", payload: null);
                            break;

                        default:
                            throw new InvalidOperationException($"Unknown verb '{op.Verb}'.");
                    }
                    results.Add(new SyncBatchResultItem(op.Id, 200, body, null));
                }
                catch (Exception ex)
                {
                    results.Add(new SyncBatchResultItem(op.Id, 500, null, ex.Message));
                }
            }

            return Results.Ok(new SyncBatchResponse(results));
        });
    }


    static readonly JsonSerializerOptions WireJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };


    static async Task<TodoItem> CreateOrReplace(IDocumentStore store, TodoItem item, bool isUpdate = false)
    {
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await store.Upsert(item);

        var payload = JsonSerializer.Serialize(item, WireJson);
        await AppendChange(store, item.Identifier, isUpdate ? "Update" : "Create", payload);

        return item;
    }


    static async Task AppendChange(IDocumentStore store, string entityId, string verb, string? payload)
    {
        await store.Insert(new SyncChange
        {
            EntityType = EntityTypeName,
            EntityId = entityId,
            Verb = verb,
            Payload = payload ?? "null",
            Timestamp = DateTimeOffset.UtcNow
        });
    }


    static DateTimeOffset TryParseCursor(string? since)
    {
        if (string.IsNullOrWhiteSpace(since))
            return DateTimeOffset.MinValue;

        return DateTimeOffset.TryParse(since, out var dt)
            ? dt
            : DateTimeOffset.MinValue;
    }


    static JsonElement ParsePayload(string raw)
    {
        try { return JsonDocument.Parse(raw).RootElement.Clone(); }
        catch { return JsonDocument.Parse("null").RootElement.Clone(); }
    }


    static JsonElement SerializeToElement<T>(T value)
        => JsonSerializer.SerializeToElement(value, WireJson);
}
