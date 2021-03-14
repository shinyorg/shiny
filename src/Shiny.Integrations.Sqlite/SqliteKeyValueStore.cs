using System;
using Shiny.Infrastructure;
using Shiny.Models;
using Shiny.Stores;


namespace Shiny.Integrations.Sqlite
{
    public class SqliteKeyValueStore : IKeyValueStore
    {
        readonly ShinySqliteConnection conn;


        public SqliteKeyValueStore(ShinySqliteConnection conn, ISerializer serializer)
        {
            this.conn = conn;
        }


        public string Alias => "sqlite";

        public bool Contains(string key)
            => this.conn.GetConnection().Find<SettingStore>(key) != null;

        public T? Get<T>(string key)
            => (T?)this.Get(typeof(T), key);

        public object? Get(Type type, string key)
            => this.conn.GetConnection().Find<SettingStore>(key)?.GetValue();

        public void Clear()
            => this.conn.GetConnection().DeleteAll<SettingStore>();

        public bool Remove(string key)
            => this.conn.GetConnection().Delete<SettingStore>(key) > 0;

        public void Set<T>(string key, T value) => this.DoSet(key, value);
        public void Set(string key, object value) => this.DoSet(key, value);

        protected virtual void DoSet(string key, object value)
        {
            var store = new SettingStore { Key = key };
            store.SetValue(value);
            this.conn.GetConnection().InsertOrReplace(store);
        }
    }
}
