#if NETSTANDARD2_1
using System;
using Shiny.Infrastructure;
using System.Text.Json;


        //public static void UseSystemTextJsonSerialization(this IServiceCollection services)
        //    => services.AddSingleton<ISerializer, SysTextJsonSerializer>();
namespace Shiny.Integrations.SysTextJson
{
    public class SysTextJsonSerializer : ISerializer
    {
        public T Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value);
        public object Deserialize(Type objectType, string value) => JsonSerializer.Serialize(value, objectType);
        public string Serialize(object value) => JsonSerializer.Serialize(value);
    }
}
#endif