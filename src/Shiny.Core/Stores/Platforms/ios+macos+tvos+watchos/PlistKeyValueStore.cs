using System;


namespace Shiny.Stores
{
    public class PlistKeyValueStore : ReadOnlyKeyValueStore
    {
        public PlistKeyValueStore() : base("plist") { }


        public override bool Contains(string key) => throw new NotImplementedException();
        public override object? Get(Type type, string key) => throw new NotImplementedException();
    }
}
