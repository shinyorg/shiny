using System;
using Shiny.Stores;

namespace Shiny;


public static class StoreExtensions
{
    static readonly object syncLock = new();


    /// <summary>
    /// Gets a value, returning defaultValue if the key is absent
    /// </summary>
    public static T Get<T>(this IKeyValueStore store, string key, T defaultValue)
    {
        var value = store.Get<T>(key);
        return value is null ? defaultValue : value;
    }


    /// <summary>
    /// If value is null or default for the type, removes the key; otherwise stores the value
    /// </summary>
    public static void SetOrRemove<T>(this IKeyValueStore store, string key, T? value)
    {
        if (value is null || value.Equals(default(T)))
            store.Remove(key);
        else
            store.Set(key, value);
    }


    /// <summary>
    /// Thread-safe incrementing counter stored at the given key
    /// </summary>
    public static int IncrementValue(this IKeyValueStore store, string key = "NextId")
    {
        lock (syncLock)
        {
            var id = store.Get<int>(key) + 1;
            store.Set(key, id);
            return id;
        }
    }


    /// <summary>
    /// Gets a required value; throws if the key is not set
    /// </summary>
    public static T GetRequired<T>(this IKeyValueStore store, string key)
    {
        var value = store.Get<T>(key);
        if (value is null)
            throw new ArgumentException($"Store key '{key}' is not set");

        return value;
    }


    /// <summary>
    /// Sets the value only if the key is not already present. Returns true if the value was set.
    /// </summary>
    public static bool SetDefault<T>(this IKeyValueStore store, string key, T value)
    {
        if (store.Contains(key))
            return false;

        store.Set(key, value);
        return true;
    }
}
