using System;
using System.Collections.Generic;

namespace Shiny.Stores.Impl;


public class MemoryKeyValueStore : IKeyValueStore
{
    public bool IsReadOnly => false;

    public void Clear() => this.Do(x => x.Clear());
    public bool Contains(string key) => this.Do(x => x.ContainsKey(key));
    public bool Remove(string key) => this.Do(x => x.Remove(key));

    public T? Get<T>(string key) => this.Do<T?>(x =>
        x.TryGetValue(key, out var val) && val is T typed ? typed : default
    );

    public void Set<T>(string key, T value) => this.Do(x =>
    {
        if (value is null)
            x.Remove(key);
        else
            x[key] = value;
    });


    Dictionary<string, object> values = new();
    readonly object syncLock = new();

    void Do(Action<Dictionary<string, object>> worker) => this.Do<object>(values =>
    {
        worker(values);
        return null!;
    });

    T Do<T>(Func<Dictionary<string, object>, T> worker)
    {
        lock (this.syncLock)
            return worker(this.values);
    }
}
