using System;
using System.Collections.Generic;
using Shiny.Infrastructure;
using Shiny.Models;
using Shiny.Settings;


namespace Shiny.Integrations.Sqlite
{
    public class SqliteSettings : AbstractSettings
    {
        readonly ShinySqliteConnection conn;
        public SqliteSettings(ShinySqliteConnection conn, ISerializer serializer) : base(serializer)
            => this.conn = conn;


        public override bool Contains(string key)
            => this.conn.GetConnection().Get<SettingStore>(key) != null;


        protected override object? NativeGet(Type type, string key)
            => this.conn.GetConnection().Get<SettingStore>(key)?.GetValue();


        protected override void NativeClear()
            => this.conn.GetConnection().DeleteAll<SettingStore>();


        protected override void NativeRemove(string[] keys)
        {
            foreach (var key in keys)
                this.conn.GetConnection().Delete<SettingStore>(key);
        }


        protected override void NativeSet(Type type, string key, object value)
        {
            var store = new SettingStore { Key = key };
            store.SetValue(value);
            this.conn.GetConnection().InsertOrReplace(store);
        }
    }
}
