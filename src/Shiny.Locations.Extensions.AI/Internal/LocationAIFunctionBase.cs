using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Shiny.Locations.Extensions.AI.Internal;

/// <summary>
/// Base for the location <see cref="AIFunction"/> tools. Holds the name/description/schema and
/// provides reflection-free argument extraction from the LLM-supplied <see cref="AIFunctionArguments"/>.
/// </summary>
abstract class LocationAIFunctionBase : AIFunction
{
    protected IGpsManager Gps { get; }
    readonly string name;
    readonly string description;
    readonly JsonElement schema;

    protected LocationAIFunctionBase(IGpsManager gps, string name, string description, JsonElement schema)
    {
        this.Gps = gps;
        this.name = name;
        this.description = description;
        this.schema = schema;
    }

    public override string Name => this.name;
    public override string Description => this.description;
    public override JsonElement JsonSchema => this.schema;

    protected static string? GetString(AIFunctionArguments args, string key)
    {
        if (!args.TryGetValue(key, out var raw) || raw is null)
            return null;
        if (raw is string s)
            return s;
        if (raw is JsonElement el)
            return el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        return raw.ToString();
    }

    protected static double? GetDouble(AIFunctionArguments args, string key)
    {
        if (!args.TryGetValue(key, out var raw) || raw is null)
            return null;
        switch (raw)
        {
            case double d: return d;
            case float f: return f;
            case int i: return i;
            case long l: return l;
            case JsonElement el when el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var v): return v;
            case string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var ps): return ps;
            default: return null;
        }
    }

    /// <summary>Resolves the last cached reading, or an error object if unavailable/denied.</summary>
    protected async Task<(GpsReading? reading, System.Text.Json.Nodes.JsonObject? error)> GetReadingAsync()
    {
        try
        {
            var reading = await this.Gps.GetLastReading().ConfigureAwait(false);
            if (reading is null)
                return (null, new System.Text.Json.Nodes.JsonObject { ["error"] = "No GPS reading is available yet. Start a GPS listener or wait for a fix." });
            return (reading, null);
        }
        catch (Exception ex)
        {
            return (null, new System.Text.Json.Nodes.JsonObject { ["error"] = $"Location is unavailable: {ex.Message}" });
        }
    }
}
