using System;

namespace Shiny.Stores;


public interface IKeyValueStore
{
    string Alias { get; }
    bool IsReadOnly { get; }

    bool Remove(string key);
    void Clear();
    bool Contains(string key);

    object? Get(Type type, string key);
    void Set(string key, object value);
}
