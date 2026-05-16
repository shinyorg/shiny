using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Shiny.Stores.Impl;


public class DefaultSerializer : ISerializer
{
    readonly JsonSerializerOptions options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };


    /// <summary>
    /// Registers a <see cref="JsonSerializerContext"/> so its types can be
    /// serialized/deserialized in an AOT-compatible way.
    /// </summary>
    public void AddContext(JsonSerializerContext context)
        => this.options.TypeInfoResolverChain.Add(context);


    public T Deserialize<T>(string value) => (T)this.Deserialize(typeof(T), value);

    public object Deserialize(Type objectType, string value)
    {
        var typeInfo = this.GetRequiredTypeInfo(objectType);
        var result = JsonSerializer.Deserialize(value, typeInfo);
        return result!;
    }


    public string Serialize(object value)
    {
        var typeInfo = this.GetRequiredTypeInfo(value.GetType());
        return JsonSerializer.Serialize(value, typeInfo);
    }


    JsonTypeInfo GetRequiredTypeInfo(Type type)
    {
        JsonTypeInfo? typeInfo = null;
        try
        {
            typeInfo = this.options.GetTypeInfo(type);
        }
        catch (InvalidOperationException) { }

        if (typeInfo == null)
        {
            throw new InvalidOperationException(
                $"No JsonTypeInfo registered for type '{type.FullName}'. " +
                $"Register a JsonSerializerContext containing this type via services.AddJsonContext(...)."
            );
        }

        return typeInfo;
    }
}
