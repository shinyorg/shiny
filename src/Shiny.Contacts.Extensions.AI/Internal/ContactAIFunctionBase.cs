using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Shiny.Contacts.Extensions.AI.Internal;

/// <summary>
/// Base for the contact <see cref="AIFunction"/> tools. Holds the name/description/schema and
/// provides reflection-free argument extraction from the LLM-supplied <see cref="AIFunctionArguments"/>.
/// </summary>
abstract class ContactAIFunctionBase : AIFunction
{
    protected IContactStore Store { get; }
    readonly string name;
    readonly string description;
    readonly JsonElement schema;

    protected ContactAIFunctionBase(IContactStore store, string name, string description, JsonElement schema)
    {
        this.Store = store;
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

    protected static int? GetInt(AIFunctionArguments args, string key)
    {
        if (!args.TryGetValue(key, out var raw) || raw is null)
            return null;
        switch (raw)
        {
            case int i: return i;
            case long l: return (int)l;
            case double d: return (int)d;
            case JsonElement el when el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v): return v;
            case string s when int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var ps): return ps;
            default: return null;
        }
    }
}
