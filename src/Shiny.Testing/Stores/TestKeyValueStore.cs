using System;
using System.Collections.Generic;
using Shiny.Stores;


namespace Shiny.Testing.Stores
{
    public class TestKeyValueStore : IKeyValueStore
    {
        public IDictionary<string, object> Values { get; set; } = new Dictionary<string, object>();


        public string Alias { get; set; } = "settings";
        public bool IsReadOnly { get; set; } = false;
        public bool Contains(string key) => this.Values.ContainsKey(key);

        public T? Get<T>(string key) => (T?)(this.Values.ContainsKey(key) ? this.Values[key] : null);
        public object? Get(Type type, string key) => this.Values[key];

        public void Set(string key, object value) => this.Values[key] = value;
        public void Clear() => this.Values.Clear();
        public bool Remove(string key) => this.Values.Remove(key);
    }
}