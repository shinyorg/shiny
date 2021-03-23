using System;
using Shiny.Infrastructure;
using Shiny.Models;
using Shiny.Stores;


namespace Shiny.Integrations.Sqlite
{
    public class SqliteKeyValueStore : IKeyValueStore
    {
        readonly ShinySqliteConnection conn;
        readonly ISerializer serializer;


        public SqliteKeyValueStore(ShinySqliteConnection conn, ISerializer serializer)
        {
            this.conn = conn;
            this.serializer = serializer;
        }


        public string Alias => "sqlite";
        public bool IsReadOnly => false;

        public bool Contains(string key)
            => this.conn.GetConnection().Find<SettingStore>(key) != null;

        public object? Get(Type type, string key)
        {
            var store = this.conn.GetConnection().Find<SettingStore>(key);
            if (store == null)
                return type.GetDefaultValue();

            var result = store.GetValue();
            if (type != typeof(string) && result is string s)
                result = this.serializer.Deserialize(type, s);

            return result;
        }

        public void Clear()
            => this.conn.GetConnection().DeleteAll<SettingStore>();

        public bool Remove(string key)
            => this.conn.GetConnection().Delete<SettingStore>(key) > 0;

        public void Set(string key, object value) => this.DoSet(key, value);

        protected virtual void DoSet(string key, object value)
        {
            var store = new SettingStore { Key = key };
            if (!store.SetValue(value))
            {
                var s = this.serializer.Serialize(value);
                store.SetValue(s);
            }
            this.conn.GetConnection().InsertOrReplace(store);
        }
    }
}
