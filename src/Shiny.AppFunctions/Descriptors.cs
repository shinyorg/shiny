using System.Text;
using System.Text.Json;

namespace Shiny.AppFunctions;

public enum AppValueKind
{
    Void,
    String,
    Int32,
    Int64,
    Double,
    Boolean,
    /// <summary>ISO 8601 string on the wire.</summary>
    DateTimeOffset,
    /// <summary>The C# member name as a string on the wire.</summary>
    Enum,
    /// <summary>An <see cref="AppEntityAttribute"/> entity; its id on the wire, resolved through <see cref="IAppEntityQuery{TEntity}"/>.</summary>
    Entity,
    Object,
    Array
}

/// <summary>The shape of a parameter, result or property. Generated; also the source of the JSON schemas.</summary>
public sealed class AppTypeDescriptor
{
    public required AppValueKind Kind { get; init; }
    public bool IsNullable { get; init; }

    /// <summary>Qualified type name for <see cref="AppValueKind.Object"/>, <see cref="AppValueKind.Enum"/> and <see cref="AppValueKind.Entity"/>.</summary>
    public string? TypeName { get; init; }

    /// <summary>For <see cref="AppValueKind.Entity"/>: the <see cref="AppEntityAttribute.Id"/>.</summary>
    public string? EntityId { get; init; }

    public IReadOnlyList<string> EnumValues { get; init; } = [];
    public IReadOnlyList<AppPropertyDescriptor> Properties { get; init; } = [];
    public AppTypeDescriptor? ItemType { get; init; }

    public static AppTypeDescriptor Void { get; } = new() { Kind = AppValueKind.Void };
}

public sealed class AppPropertyDescriptor
{
    /// <summary>Wire name (camelCase).</summary>
    public required string Name { get; init; }
    public required AppTypeDescriptor Type { get; init; }
}

public sealed class AppParameterDescriptor
{
    /// <summary>Wire name (camelCase). Also the Swift property and the Android GenericDocument property name.</summary>
    public required string Name { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required AppTypeDescriptor Type { get; init; }
    public bool IsRequired { get; init; }
}

public sealed class AppFunctionDescriptor
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public bool OpensApp { get; init; }
    public IReadOnlyList<AppParameterDescriptor> Parameters { get; init; } = [];
    public required AppTypeDescriptor Result { get; init; }

    /// <summary>Set for the generated <c>search_{entity}</c> functions, which run <see cref="IAppEntityQuery{TEntity}.Search"/>.</summary>
    public string? SearchesEntityId { get; init; }

    /// <summary>JSON schema (draft 2020-12) for the parameters, for AI tool / MCP adapters.</summary>
    public string GetParametersJsonSchema()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "object");
            writer.WriteStartObject("properties");
            foreach (var p in this.Parameters)
            {
                writer.WritePropertyName(p.Name);
                WriteSchema(writer, p.Type, p.Description ?? p.Title);
            }
            writer.WriteEndObject();
            writer.WriteStartArray("required");
            foreach (var p in this.Parameters.Where(x => x.IsRequired))
                writer.WriteStringValue(p.Name);
            writer.WriteEndArray();
            writer.WriteBoolean("additionalProperties", false);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    static void WriteSchema(Utf8JsonWriter writer, AppTypeDescriptor type, string? description)
    {
        writer.WriteStartObject();
        switch (type.Kind)
        {
            case AppValueKind.String:
                writer.WriteString("type", "string");
                break;
            case AppValueKind.Int32:
            case AppValueKind.Int64:
                writer.WriteString("type", "integer");
                break;
            case AppValueKind.Double:
                writer.WriteString("type", "number");
                break;
            case AppValueKind.Boolean:
                writer.WriteString("type", "boolean");
                break;
            case AppValueKind.DateTimeOffset:
                writer.WriteString("type", "string");
                writer.WriteString("format", "date-time");
                break;
            case AppValueKind.Enum:
                writer.WriteString("type", "string");
                writer.WriteStartArray("enum");
                foreach (var v in type.EnumValues)
                    writer.WriteStringValue(v);
                writer.WriteEndArray();
                break;
            case AppValueKind.Entity:
                writer.WriteString("type", "string");
                description = $"{description} (the id of a {type.EntityId}; find one with search_{type.EntityId})";
                break;
            case AppValueKind.Array:
                writer.WriteString("type", "array");
                writer.WritePropertyName("items");
                WriteSchema(writer, type.ItemType!, null);
                break;
            case AppValueKind.Object:
                writer.WriteString("type", "object");
                writer.WriteStartObject("properties");
                foreach (var p in type.Properties)
                {
                    writer.WritePropertyName(p.Name);
                    WriteSchema(writer, p.Type, null);
                }
                writer.WriteEndObject();
                break;
        }
        if (!String.IsNullOrWhiteSpace(description))
            writer.WriteString("description", description);
        writer.WriteEndObject();
    }
}

public sealed class AppEntityDescriptor
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string TypeName { get; init; }
}

/// <summary>An entity as the assistant sees it: its id and display text.</summary>
public readonly record struct AppEntityItem(string Id, string Title);

public enum AppEntityQueryKind
{
    ByIds,
    Search,
    Suggested
}
