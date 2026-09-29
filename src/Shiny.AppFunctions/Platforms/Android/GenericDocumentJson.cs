using System.Text;
using System.Text.Json;
using Android.App.AppFunctions;
using Android.App.AppSearch;

namespace Shiny.AppFunctions;

/// <summary>
/// Converts between AppFunctions' GenericDocuments and the dispatcher's JSON, driven by the generated descriptors
/// (the same ones app_functions_v2.xml is written from), so it needs no per-type code and no reflection.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("android36.0")]
static class GenericDocumentJson
{
    /// <summary>Request parameters → a JSON object keyed by parameter name. Missing properties are left out.</summary>
    public static string ToJson(GenericDocument? parameters, AppFunctionDescriptor function)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            if (parameters != null)
            {
                var present = parameters.PropertyNames.ToHashSet();
                foreach (var p in function.Parameters.Where(x => present.Contains(x.Name)))
                {
                    writer.WritePropertyName(p.Name);
                    WriteValue(writer, parameters, p.Name, p.Type.Kind);
                }
                // generated search_{entity} functions take a single "query"
                if (function.SearchesEntityId != null && present.Contains("query"))
                {
                    writer.WritePropertyName("query");
                    WriteValue(writer, parameters, "query", AppValueKind.String);
                }
            }
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    static void WriteValue(Utf8JsonWriter writer, GenericDocument doc, string name, AppValueKind kind)
    {
        switch (kind)
        {
            case AppValueKind.Int32:
            case AppValueKind.Int64:
                writer.WriteNumberValue(doc.GetPropertyLong(name));
                break;
            case AppValueKind.Double:
                writer.WriteNumberValue(doc.GetPropertyDouble(name));
                break;
            case AppValueKind.Boolean:
                writer.WriteBooleanValue(doc.GetPropertyBoolean(name));
                break;
            default: // String, DateTimeOffset, Enum, Entity (id)
                var s = doc.GetPropertyString(name);
                if (s == null)
                    writer.WriteNullValue();
                else
                    writer.WriteStringValue(s);
                break;
        }
    }

    /// <summary>The dispatcher's JSON result → the response document (value under <c>androidAppfunctionsReturnValue</c>).</summary>
    public static GenericDocument ToResultDocument(string? resultJson, AppTypeDescriptor resultType)
    {
        var builder = new GenericDocument.Builder("", "", "");
        if (resultJson != null && resultType.Kind != AppValueKind.Void)
        {
            using var json = JsonDocument.Parse(resultJson);
            SetProperty(builder, ExecuteAppFunctionResponse.PropertyReturnValue, resultType, json.RootElement);
        }
        return builder.Build();
    }

    static void SetProperty(GenericDocument.Builder builder, string name, AppTypeDescriptor type, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
            return;

        switch (type.Kind)
        {
            case AppValueKind.Array:
                SetArray(builder, name, type.ItemType!, value);
                break;
            case AppValueKind.Object:
                builder.SetPropertyDocument(name, [ToObjectDocument(type, value)]);
                break;
            case AppValueKind.Int32:
            case AppValueKind.Int64:
                builder.SetPropertyLong(name, [value.GetInt64()]);
                break;
            case AppValueKind.Double:
                builder.SetPropertyDouble(name, [value.GetDouble()]);
                break;
            case AppValueKind.Boolean:
                builder.SetPropertyBoolean(name, [value.GetBoolean()]);
                break;
            default:
                builder.SetPropertyString(name, [value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText()]);
                break;
        }
    }

    static void SetArray(GenericDocument.Builder builder, string name, AppTypeDescriptor itemType, JsonElement array)
    {
        var items = array.EnumerateArray().Where(x => x.ValueKind != JsonValueKind.Null).ToList();
        switch (itemType.Kind)
        {
            case AppValueKind.Object:
                builder.SetPropertyDocument(name, items.Select(x => ToObjectDocument(itemType, x)).ToArray());
                break;
            case AppValueKind.Int32:
            case AppValueKind.Int64:
                builder.SetPropertyLong(name, items.Select(x => x.GetInt64()).ToArray());
                break;
            case AppValueKind.Double:
                builder.SetPropertyDouble(name, items.Select(x => x.GetDouble()).ToArray());
                break;
            case AppValueKind.Boolean:
                builder.SetPropertyBoolean(name, items.Select(x => x.GetBoolean()).ToArray());
                break;
            default:
                builder.SetPropertyString(name, items.Select(x => x.ValueKind == JsonValueKind.String ? x.GetString()! : x.GetRawText()).ToArray());
                break;
        }
    }

    static GenericDocument ToObjectDocument(AppTypeDescriptor type, JsonElement value)
    {
        var builder = new GenericDocument.Builder("", "", type.TypeName ?? "");
        foreach (var p in type.Properties)
        {
            if (value.TryGetProperty(p.Name, out var v))
                SetProperty(builder, p.Name, p.Type, v);
        }
        return builder.Build();
    }
}
