using System;
using Newtonsoft.Json;


namespace Shiny.Infrastructure
{
    public class JsonNetSerializer : ISerializer
    {
        readonly JsonSerializerSettings settings;

        public JsonNetSerializer() { }

        public JsonNetSerializer(JsonSerializerSettings settings) => this.settings = settings;

        public T Deserialize<T>(string value) => JsonConvert.DeserializeObject<T>(value, this.settings);
        public object Deserialize(Type objectType, string value) => JsonConvert.DeserializeObject(value, objectType, this.settings);
        public string Serialize(object value) => JsonConvert.SerializeObject(value, this.settings);
    }
}
