using System;
using Shiny.Infrastructure;
using System.Text.Json.Serialization;


namespace Shiny
{
    public class Serializer : ISerializer
    {
        public T Deserialize<T>(string value) => JsonSerializer.Parse<T>(value);
        public object Deserialize(Type objectType, string value) => JsonSerializer.Parse(value, objectType);
        public string Serialize(object value) => JsonSerializer.ToString(value);
    }
}
