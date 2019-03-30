using System;
using Microsoft.JSInterop;


namespace Acr.Infrastructure
{
    public class BlazorJsonSerializer : ISerializer
    {
        public T Deserialize<T>(string value) => Json.Deserialize<T>(value);

        public object Deserialize(Type objectType, string value)
        {
            //Json.Deserialize<>()
            throw new NotImplementedException();
        }

        public string Serialize(object value) => Json.Serialize(value);
    }
}
