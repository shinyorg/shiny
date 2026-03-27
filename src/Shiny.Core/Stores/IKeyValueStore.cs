using System;

namespace Shiny.Stores;


/// <summary>
/// Provides key-value storage operations for persisting application settings and state
/// </summary>
public interface IKeyValueStore
{
    /// <summary>
    /// Gets the unique alias identifying this store
    /// </summary>
    string Alias { get; }

    /// <summary>
    /// Gets whether this store is read-only
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Removes a value by key
    /// </summary>
    /// <param name="key">The key to remove</param>
    /// <returns>True if the key was found and removed</returns>
    bool Remove(string key);

    /// <summary>
    /// Removes all values from the store
    /// </summary>
    void Clear();

    /// <summary>
    /// Checks whether the store contains a value for the specified key
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <returns>True if the key exists</returns>
    bool Contains(string key);

    /// <summary>
    /// Gets a value by type and key
    /// </summary>
    /// <param name="type">The expected type of the value</param>
    /// <param name="key">The key to retrieve</param>
    /// <returns>The stored value, or null if not found</returns>
    object? Get(Type type, string key);

    /// <summary>
    /// Sets a value for the specified key
    /// </summary>
    /// <param name="key">The key to store</param>
    /// <param name="value">The value to store</param>
    void Set(string key, object value);
}
