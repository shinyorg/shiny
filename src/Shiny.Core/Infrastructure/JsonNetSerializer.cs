using System;
using Newtonsoft.Json;


namespace Shiny.Infrastructure
{
    public class JsonNetSerializer : ISerializer
    {
        public T Deserialize<T>(string value) => JsonConvert.DeserializeObject<T>(value);
        public object Deserialize(Type objectType, string value) => JsonConvert.DeserializeObject(value, objectType);
        public string Serialize(object value) => JsonConvert.SerializeObject(value);
    }
}
