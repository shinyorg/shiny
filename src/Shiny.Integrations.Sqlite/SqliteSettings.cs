using System;
using System.Collections.Generic;
using Shiny.Infrastructure;
using Shiny.Settings;


namespace Shiny
{
    class SqliteSettings : AbstractSettings
    {
        readonly ShinySqliteConnection conn;
        public SqliteSettings(ShinySqliteConnection conn, ISerializer serializer) : base(serializer)
            => this.conn = conn;


        public override bool Contains(string key)
        {
            throw new NotImplementedException();
        }

        protected override object NativeGet(Type type, string key)
        {
            throw new NotImplementedException();
        }

        protected override void NativeRemove(string[] keys)
        {
            throw new NotImplementedException();
        }

        protected override void NativeSet(Type type, string key, object value)
        {
            throw new NotImplementedException();
        }

        protected override IDictionary<string, string> NativeValues()
        {
            throw new NotImplementedException();
        }
    }
}
