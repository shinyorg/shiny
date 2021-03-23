using System;


namespace Shiny.Stores
{
    public class AppConfigKeyValueStore : ReadOnlyKeyValueStore
    {
        public AppConfigKeyValueStore() : base("appconfig") { }

        public override bool Contains(string key) => throw new NotImplementedException();
        public override object? Get(Type type, string key) => throw new NotImplementedException();
    }
}
