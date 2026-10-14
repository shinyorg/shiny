using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Shiny.DeviceTests.Runner.Fixtures;


/// <summary>
/// /sync/* - an in-memory server for Shiny.Data.Sync's RestSyncTransport wire contract:
///   POST {url}           create        PUT {url}/{id}  update        DELETE {url}/{id}  delete
///   GET  {url}?since=c   pull          -> { cursor, hasMore, items: [{ id, verb, payload }] }
///   GET  {url}/tombstones?since=c      -> { cursor, ids: [...] }
/// 409 answers carry the server's current entity, as the engine hands that body to OnConflict.
/// /sync/test/* are the hooks the DataSync suite uses to seed changes, inject failures and read what arrived.
/// Cursors are "{epoch}:{sequence}" - a reset starts a new epoch, so a cursor the app persisted before the
/// reset reads the feed from the start instead of skipping it.
/// </summary>
public static class DataSyncFixture
{
    static readonly object sync = new();
    static readonly Dictionary<string, string> entities = new();
    static readonly List<(long Seq, string Id, string Verb, string Payload)> feed = [];
    static readonly List<(long Seq, string Id)> tombstones = [];
    static readonly List<SyncWrite> writes = [];
    static readonly List<string?> pulls = [];
    static readonly Dictionary<string, (int Status, int Remaining)> failures = new();
    static readonly Dictionary<string, int> conflicts = new();
    static string epoch = NewEpoch();
    static long sequence;
    static int pageSize = 100;


    public static void MapDataSync(this WebApplication app, RunnerState state)
    {
        app.MapGet("/sync/notes", (HttpContext ctx) =>
        {
            var since = ctx.Request.Query["since"].FirstOrDefault();
            lock (sync)
            {
                pulls.Add(since);
                var from = ParseCursor(since);
                var page = feed.Where(x => x.Seq > from).OrderBy(x => x.Seq).Take(pageSize).ToList();
                var last = page.Count > 0 ? page[^1].Seq : Math.Max(from, 0);
                var hasMore = feed.Any(x => x.Seq > last);

                var items = new JsonArray();
                foreach (var item in page)
                {
                    items.Add(new JsonObject
                    {
                        ["id"] = item.Id,
                        ["verb"] = item.Verb,
                        ["payload"] = JsonNode.Parse(item.Payload)
                    });
                }
                var body = new JsonObject
                {
                    ["cursor"] = Cursor(last),
                    ["hasMore"] = hasMore,
                    ["items"] = items
                };
                return Results.Text(body.ToJsonString(), "application/json");
            }
        });

        app.MapGet("/sync/notes/tombstones", (HttpContext ctx) =>
        {
            var from = ParseCursor(ctx.Request.Query["since"].FirstOrDefault());
            lock (sync)
            {
                var page = tombstones.Where(x => x.Seq > from).ToList();
                var last = page.Count > 0 ? page.Max(x => x.Seq) : Math.Max(from, 0);
                var body = new JsonObject
                {
                    ["cursor"] = Cursor(last),
                    ["ids"] = new JsonArray(page.Select(x => (JsonNode)JsonValue.Create(x.Id)!).ToArray())
                };
                return Results.Text(body.ToJsonString(), "application/json");
            }
        });

        app.MapPost("/sync/notes", async (HttpContext ctx) =>
        {
            var payload = await ReadBodyAsync(ctx);
            var id = GetIdentifier(payload) ?? Guid.NewGuid().ToString("N");
            return Write(ctx, id, "Create", payload);
        });

        app.MapPut("/sync/notes/{id}", async (HttpContext ctx, string id) =>
            Write(ctx, id, "Update", await ReadBodyAsync(ctx)));

        app.MapDelete("/sync/notes/{id}", (HttpContext ctx, string id) => Write(ctx, id, "Delete", null));

        // ── test hooks ──
        app.MapPost("/sync/test/reset", () =>
        {
            lock (sync)
            {
                entities.Clear();
                feed.Clear();
                tombstones.Clear();
                writes.Clear();
                pulls.Clear();
                failures.Clear();
                conflicts.Clear();
                pageSize = 100;
                epoch = NewEpoch();
            }
            return Results.Ok();
        });

        // a server-side change for the client to pull - body is the entity JSON, ?verb=Create|Update|Delete
        app.MapPost("/sync/test/seed/{id}", async (HttpContext ctx, string id) =>
        {
            var payload = await ReadBodyAsync(ctx);
            var verb = ctx.Request.Query["verb"].FirstOrDefault() ?? "Create";
            lock (sync)
            {
                if (verb.Equals("Delete", StringComparison.OrdinalIgnoreCase))
                    entities.Remove(id);
                else
                    entities[id] = payload;

                feed.Add((++sequence, id, verb, payload));
            }
            return Results.Ok();
        });

        // an entity the server holds without announcing it on the feed (conflict baseline)
        app.MapPut("/sync/test/entity/{id}", async (HttpContext ctx, string id) =>
        {
            var payload = await ReadBodyAsync(ctx);
            lock (sync)
                entities[id] = payload;
            return Results.Ok();
        });

        app.MapGet("/sync/test/entity/{id}", (string id) =>
        {
            lock (sync)
            {
                return entities.TryGetValue(id, out var payload)
                    ? Results.Text(payload, "application/json")
                    : Results.NotFound();
            }
        });

        app.MapPost("/sync/test/tombstone/{id}", (string id) =>
        {
            lock (sync)
            {
                entities.Remove(id);
                tombstones.Add((++sequence, id));
            }
            return Results.Ok();
        });

        // the next {count} writes for {id} answer {status}
        app.MapPost("/sync/test/fail/{id}", (string id, int status, int count) =>
        {
            lock (sync)
                failures[id] = (status, count);
            return Results.Ok();
        });

        // the next {count} writes for {id} answer 409 with the server's current entity
        app.MapPost("/sync/test/conflict/{id}", (string id, int count) =>
        {
            lock (sync)
                conflicts[id] = count;
            return Results.Ok();
        });

        app.MapPost("/sync/test/pagesize/{size:int}", (int size) =>
        {
            lock (sync)
                pageSize = Math.Max(1, size);
            return Results.Ok();
        });

        app.MapGet("/sync/test/writes", () =>
        {
            lock (sync)
                return Results.Json(writes.ToList(), DataSyncFixtureJsonContext.Default.ListSyncWrite);
        });

        app.MapGet("/sync/test/pulls", () =>
        {
            lock (sync)
                return Results.Json(pulls.ToList(), DataSyncFixtureJsonContext.Default.ListString);
        });
    }


    static IResult Write(HttpContext ctx, string id, string verb, string? payload)
    {
        lock (sync)
        {
            var attempt = writes.Count(x => x.EntityId == id) + 1;

            if (failures.TryGetValue(id, out var failure) && failure.Remaining > 0)
            {
                failures[id] = failure with { Remaining = failure.Remaining - 1 };
                writes.Add(new SyncWrite(id, ctx.Request.Method, verb, payload, failure.Status, attempt));
                return Results.Text($"injected failure {failure.Status}", statusCode: failure.Status);
            }

            if (conflicts.TryGetValue(id, out var remaining) && remaining > 0)
            {
                conflicts[id] = remaining - 1;
                writes.Add(new SyncWrite(id, ctx.Request.Method, verb, payload, 409, attempt));
                var current = entities.TryGetValue(id, out var existing) ? existing : "{}";
                return Results.Text(current, "application/json", statusCode: 409);
            }

            if (verb == "Delete")
                entities.Remove(id);
            else
                entities[id] = payload ?? "{}";

            var status = verb == "Create" ? 201 : 200;
            writes.Add(new SyncWrite(id, ctx.Request.Method, verb, payload, status, attempt));
            return verb == "Delete"
                ? Results.StatusCode(status)
                : Results.Text(payload ?? "{}", "application/json", statusCode: status);
        }
    }


    static async Task<string> ReadBodyAsync(HttpContext ctx)
    {
        using var reader = new StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync(ctx.RequestAborted);
        return String.IsNullOrWhiteSpace(body) ? "{}" : body;
    }


    // the entity's own identifier - the engine only puts it in the URL for PUT/DELETE
    static string? GetIdentifier(string payload)
    {
        if (JsonNode.Parse(payload) is not JsonObject obj)
            return null;

        return obj
            .Where(x => x.Key.Equals("identifier", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value?.GetValue<string>())
            .FirstOrDefault();
    }


    static string Cursor(long seq) => $"{epoch}:{seq.ToString(CultureInfo.InvariantCulture)}";

    static long ParseCursor(string? cursor)
    {
        if (cursor == null)
            return 0;

        var parts = cursor.Split(':');
        if (parts.Length != 2 || parts[0] != epoch || !Int64.TryParse(parts[1], CultureInfo.InvariantCulture, out var seq))
            return 0;

        return seq;
    }

    static string NewEpoch() => Guid.NewGuid().ToString("N")[..8];
}


public record SyncWrite(string EntityId, string Method, string Verb, string? Payload, int Status, int Attempt);


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<SyncWrite>))]
[JsonSerializable(typeof(List<string>))]
public partial class DataSyncFixtureJsonContext : JsonSerializerContext;
