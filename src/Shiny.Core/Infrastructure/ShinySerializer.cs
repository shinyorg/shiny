using System;
using Newtonsoft.Json;
//using System.Text.Json;


namespace Shiny.Infrastructure
{
    public class ShinySerializer : ISerializer
    {
        public T Deserialize<T>(string value) => JsonConvert.DeserializeObject<T>(value);
        public object Deserialize(Type objectType, string value) => JsonConvert.DeserializeObject(value, objectType);
        public string Serialize(object value) => JsonConvert.SerializeObject(value);
        //public T Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value);
        //public object Deserialize(Type objectType, string value) => JsonSerializer.Serialize(value, objectType);
        //public string Serialize(object value) => JsonSerializer.Serialize(value);
    }
}