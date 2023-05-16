using System;
using System.Collections.Generic;

namespace Shiny.Stores.Impl;


public class MemoryKeyValueStore : IKeyValueStore
{
    public string Alias => "memory";
    public bool IsReadOnly => false;

    public void Clear() => this.Do(x => x.Clear());
    public bool Contains(string key) => this.Do(x => x.ContainsKey(key));
    public object? Get(Type type, string key) => this.Do(x => x.ContainsKey(key) ? x[key] : null);
    public bool Remove(string key) => this.Do(x => x.Remove(key));
    public void Set(string key, object value) => this.Do(x => x[key] = value);


    Dictionary<string, object> values = new Dictionary<string, object>();
    readonly object syncLock = new object();
    protected void Do(Action<Dictionary<string, object>> worker) => this.Do<object>(values =>
    {
        worker(values);
        return null!;
    });
    protected T Do<T>(Func<Dictionary<string, object>, T> worker)
    {
        lock (this.syncLock)
        {
            return worker(this.values);
        }
    }
}
