using System;
using System.Text.Json;

namespace Shiny.Stores.Infrastructure;


public class DefaultSerializer : ISerializer
{
    public T Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value)!;
    public object Deserialize(Type objectType, string value) => JsonSerializer.Deserialize(value, objectType);
    public string Serialize(object value) => JsonSerializer.Serialize(value);
}
