using System;
namespace Shiny.Stores.Infrastructure;


public interface ISerializer
{
    T Deserialize<T>(string value);
    object Deserialize(Type objectType, string value);
    string Serialize(object value);
}
