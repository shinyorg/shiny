namespace Shiny.Stores;


/// <summary>
/// Provides key-value storage operations for persisting application settings and state
/// </summary>
public interface IKeyValueStore
{
    /// <summary>
    /// Gets whether this store is read-only
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Removes a value by key. Returns true if the key existed.
    /// </summary>
    bool Remove(string key);

    /// <summary>
    /// Removes all values from the store
    /// </summary>
    void Clear();

    /// <summary>
    /// Checks whether the store contains a value for the specified key
    /// </summary>
    bool Contains(string key);

    /// <summary>
    /// Gets a value by key. Returns default(T) if not found.
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// Sets a value for the specified key
    /// </summary>
    void Set<T>(string key, T value);
}
