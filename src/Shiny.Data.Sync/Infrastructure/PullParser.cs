using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Shiny.Data.Sync.Infrastructure;


/// <summary>
/// Parses the standard delta-pull response shape:
/// <c>{ "cursor": "...", "hasMore": false, "items": [{ "id": "...", "verb": "Create|Update|Delete", "payload": {...} }, ...] }</c>.
/// Apps that need a different shape can implement <see cref="ISyncTransport"/> directly.
/// </summary>
internal static class PullParser
{
    public static SyncPullResult Parse(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return new SyncPullResult(null, Array.Empty<SyncPullItem>());

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        string? cursor = null;
        if (root.TryGetProperty("cursor", out var c) && c.ValueKind == JsonValueKind.String)
            cursor = c.GetString();

        var hasMore = root.TryGetProperty("hasMore", out var hm)
            && hm.ValueKind is JsonValueKind.True or JsonValueKind.False
            && hm.GetBoolean();

        var items = new List<SyncPullItem>();
        if (root.TryGetProperty("items", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
            {
                var id = el.GetProperty("id").GetString()!;
                var verbStr = el.GetProperty("verb").GetString()!;
                var verb = Enum.Parse<SyncVerb>(verbStr, ignoreCase: true);
                var payload = el.GetProperty("payload").GetRawText();
                items.Add(new SyncPullItem(id, verb, payload));
            }
        }

        return new SyncPullResult(cursor, items, hasMore);
    }
}
