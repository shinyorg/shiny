using System;


namespace Shiny.Infrastructure
{
    public interface ISerializer
    {
        //public T Deserialize<T>(string value) => (T)this.Deserialize(typeof(T), value);
        T Deserialize<T>(string value);
        object Deserialize(Type objectType, string value);
        string Serialize(object value);
    }
}
