using System;


namespace Shiny.Stores
{
    public interface IKeyValueStore
    {
        string Alias { get; }

        bool Remove(string key);
        void Clear();
        bool Contains(string key);

        T? Get<T>(string key);
        object? Get(Type type, string key);

        //void Set<T>(string key, T value);
        void Set(string key, object value);
    }
}
