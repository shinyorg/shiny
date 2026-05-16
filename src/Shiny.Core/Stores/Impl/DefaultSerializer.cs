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


    public T Deserialize<T>(string value)
    {
        var typeInfo = this.GetRequiredTypeInfo<T>();
        return (T)JsonSerializer.Deserialize(value, typeInfo)!;
    }


    public string Serialize<T>(T value)
    {
        var typeInfo = this.GetRequiredTypeInfo<T>();
        return JsonSerializer.Serialize(value, typeInfo);
    }


    JsonTypeInfo GetRequiredTypeInfo<T>()
    {
        JsonTypeInfo? typeInfo = null;
        try
        {
            typeInfo = this.options.GetTypeInfo(typeof(T));
        }
        catch (InvalidOperationException) { }

        if (typeInfo == null)
        {
            throw new InvalidOperationException(
                $"No JsonTypeInfo registered for type '{typeof(T).FullName}'. " +
                $"Register a JsonSerializerContext containing this type via services.AddJsonContext(...)."
            );
        }

        return typeInfo;
    }
}
