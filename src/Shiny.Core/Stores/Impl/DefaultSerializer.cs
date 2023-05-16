using System;
using System.Text.Json;

namespace Shiny.Stores.Impl;


public class DefaultSerializer : ISerializer
{
    readonly JsonSerializerOptions serializeOptions = new()
    {
        Converters =
        {
            new TypeJsonConverter()
        }
    };


    public T Deserialize<T>(string value) => (T)this.Deserialize(typeof(T), value);
    public object Deserialize(Type objectType, string value)
    {
        var result = JsonSerializer.Deserialize(value, objectType, this.serializeOptions);
        return result!;
    }


    public string Serialize(object value) => JsonSerializer.Serialize(value, this.serializeOptions);
}
