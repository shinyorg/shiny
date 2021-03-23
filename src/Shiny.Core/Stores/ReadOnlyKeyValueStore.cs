using System;


namespace Shiny.Stores
{
    public abstract class ReadOnlyKeyValueStore : IKeyValueStore
    {
        protected ReadOnlyKeyValueStore(string alias) => this.Alias = alias;
        public string Alias { get; }
        public abstract object? Get(Type type, string key);
        public abstract bool Contains(string key);


        public bool IsReadOnly => true;
        public void Clear() => throw new NotImplementedException("This store is readonly");
        public bool Remove(string key) => throw new NotImplementedException("This store is readonly");
        public void Set(string key, object value) => throw new NotImplementedException("This store is readonly");
    }
}
