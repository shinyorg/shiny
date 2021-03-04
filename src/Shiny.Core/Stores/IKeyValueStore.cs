using System;


namespace Shiny.Stores
{
    public interface IKeyValueStore
    {
        bool Remove(string key);
        void Clear();
        bool Contains(string key);

        T? Get<T>(string key);
        object? Get(Type type, string key);

        void Set<T>(string key, T value);
        void Set(string key, object value);
    }
}
