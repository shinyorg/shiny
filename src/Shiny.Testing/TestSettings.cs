using System;
using System.Collections.Generic;
using System.Linq;
using Shiny.Infrastructure;
using Shiny.Settings;


namespace Shiny.Testing.Settings
{
    /// <summary>
    /// This is a simple (non-thread safe) dictionary useful for unit testing.  NOT INTENDED FOR PRODUCTION!
    /// </summary>
    public class InMemorySettings : AbstractSettings
    {
        public InMemorySettings(ISerializer serializer) : base(serializer) {}


        public IDictionary<string, object> Values { get; set; } = new Dictionary<string, object>();


        protected override IDictionary<string, string> NativeValues()
            => this.Values.ToDictionary(
                x => x.Key,
                x => x.Value.ToString()
            );
        public override bool Contains(string key) => this.Values.ContainsKey(key);
        protected override object NativeGet(Type type, string key) => this.Values[key];
        protected override void NativeSet(Type type, string key, object value) => this.Values[key] = value;


        protected override void NativeRemove(string[] keys)
        {
            foreach (var key in keys)
                this.Values.Remove(key);
        }
    }
}