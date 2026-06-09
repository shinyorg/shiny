using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Shiny.Data.Sync.Infrastructure;


/// <summary>
/// Parses the two server shapes the engine accepts for a tombstone response:
/// <list type="bullet">
///   <item><c>["id1","id2",...]</c> — a flat array of IDs (no cursor).</item>
///   <item><c>{ "cursor": "...", "ids": ["id1","id2",...] }</c> — paginated/cursored.</item>
/// </list>
/// </summary>
internal static class TombstoneParser
{
    public static SyncTombstoneResult Parse(string? body, string? fallbackCursor)
    {
        if (string.IsNullOrWhiteSpace(body))
            return new SyncTombstoneResult(fallbackCursor, Array.Empty<string>());

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
            return new SyncTombstoneResult(fallbackCursor, ReadIds(root));

        if (root.ValueKind == JsonValueKind.Object)
        {
            string? cursor = fallbackCursor;
            if (root.TryGetProperty("cursor", out var c) && c.ValueKind == JsonValueKind.String)
                cursor = c.GetString();

            if (root.TryGetProperty("ids", out var arr) && arr.ValueKind == JsonValueKind.Array)
                return new SyncTombstoneResult(cursor, ReadIds(arr));

            return new SyncTombstoneResult(cursor, Array.Empty<string>());
        }

        return new SyncTombstoneResult(fallbackCursor, Array.Empty<string>());
    }


    static IReadOnlyList<string> ReadIds(JsonElement arr)
    {
        var list = new List<string>();
        foreach (var el in arr.EnumerateArray())
        {
            if (el.ValueKind == JsonValueKind.String)
            {
                var s = el.GetString();
                if (!string.IsNullOrEmpty(s))
                    list.Add(s);
            }
        }
        return list;
    }
}
