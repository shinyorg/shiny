using System;
using System.Collections.Generic;


namespace Shiny.Stores
{
    public class MemoryKeyValueStore : IKeyValueStore
    {
        public void Clear() => this.Do(x => x.Clear());
        public bool Contains(string key) => this.Do(x => x.ContainsKey(key));
        public T? Get<T>(string key) => (T?)this.Get(typeof(T), key);
        public object Get(Type type, string key) => this.Do(x => x.ContainsKey(key) ? x[key] : null);
        public bool Remove(string key) => (bool)this.Do(x => x.Remove(key));
        public void Set<T>(string key, T value) => this.Do(x => x[key] = value);
        public void Set(string key, object value) => this.Do(x => x[key] = value);


        Dictionary<string, object> values = new Dictionary<string, object>();
        readonly object syncLock = new object();
        protected void Do(Action<Dictionary<string, object>> worker) => this.Do<object>(values =>
        {
            worker(values);
            return null;
        });
        protected T Do<T>(Func<Dictionary<string, object>, T> worker)
        {
            lock (this.syncLock)
            {
                return (T)worker(this.values);
            }
        }
    }
}
