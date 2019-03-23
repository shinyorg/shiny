using System;


namespace Shiny.Caching
{
    public class VoidCacheImpl : AbstractCache
    {
        protected override void Init() { }
        public override T Get<T>(string key) => default(T);
        public override void Set(string key, object obj, TimeSpan? timeSpan = null) { }
        public override void Clear() { }
        public override bool Remove(string key) => false;
    }
}
