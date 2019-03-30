using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Shiny.Caching
{
    public class VoidCache : AbstractCache
    {
        protected override void Init() { }
        public override Task<T> Get<T>(string key) => Task.FromResult(default(T));
        public override Task Set(string key, object obj, TimeSpan? timeSpan = null) => Task.CompletedTask;
        public override Task Clear() => Task.CompletedTask;
        public override Task<bool> Remove(string key) => Task.FromResult(false);
        public override Task<IEnumerable<CacheItem>> GetCachedItems() => Task.FromResult(Enumerable.Empty<CacheItem>());
    }
}
