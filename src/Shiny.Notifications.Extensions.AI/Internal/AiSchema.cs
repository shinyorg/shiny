using System.Text.Json;
using System.Text.Json.Nodes;

namespace Shiny.Notifications.Extensions.AI.Internal;

/// <summary>Helpers for hand-building JSON Schema fragments (reflection-free / AOT-safe).</summary>
static class AiSchema
{
    public static JsonElement ToElement(JsonNode node)
    {
        using var doc = JsonDocument.Parse(node.ToJsonString());
        return doc.RootElement.Clone();
    }

    public static JsonObject Object(JsonObject properties, params string[] required)
    {
        var o = new JsonObject { ["type"] = "object", ["properties"] = properties };
        if (required.Length > 0)
        {
            var arr = new JsonArray();
            foreach (var r in required)
                arr.Add((JsonNode)r);
            o["required"] = arr;
        }
        return o;
    }

    public static JsonObject String(string description, IEnumerable<string>? enumValues = null)
    {
        var o = new JsonObject { ["type"] = "string", ["description"] = description };
        if (enumValues != null)
        {
            var arr = new JsonArray();
            foreach (var v in enumValues)
                arr.Add((JsonNode)v);
            o["enum"] = arr;
        }
        return o;
    }

    public static JsonObject Number(string description) => new() { ["type"] = "number", ["description"] = description };

    public static JsonObject Integer(string description) => new() { ["type"] = "integer", ["description"] = description };

    public static JsonObject Boolean(string description) => new() { ["type"] = "boolean", ["description"] = description };

    public static JsonObject Date(string role) => new()
    {
        ["type"] = "string",
        ["description"] = $"{role} as an ISO-8601 date or datetime (e.g. \"2026-07-06\" or \"2026-07-06T08:00:00Z\")."
    };
}
