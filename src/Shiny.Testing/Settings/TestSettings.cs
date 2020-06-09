using System;
using System.Collections.Generic;
using Shiny.Infrastructure;
using Shiny.Settings;


namespace Shiny.Testing.Settings
{
    public class TestSettings : AbstractSettings
    {
        public TestSettings(ISerializer serializer) : base(serializer) {}


        public IDictionary<string, object> Values { get; set; } = new Dictionary<string, object>();


        public override bool Contains(string key) => this.Values.ContainsKey(key);
        protected override object NativeGet(Type type, string key) => this.Values[key];
        protected override void NativeSet(Type type, string key, object value) => this.Values[key] = value;
        protected override void NativeClear() => this.Values.Clear();
        protected override void NativeRemove(string[] keys)
        {
            foreach (var key in keys)
                this.Values.Remove(key);
        }
    }
}