using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Shiny.Caching
{
    public class MemoryCache : AbstractTimerCache
    {
        readonly IDictionary<string, CacheItem> cache = new Dictionary<string, CacheItem>();
        readonly object syncLock = new object();


        public MemoryCache(TimeSpan? defaultLifeSpan = null, TimeSpan? cleanUpTimer = null)
        {
            this.DefaultLifeSpan = defaultLifeSpan ?? TimeSpan.FromMinutes(10);
            this.CleanUpTime = cleanUpTimer ?? TimeSpan.FromSeconds(20);
        }


        public override Task<IEnumerable<CacheItem>> GetCachedItems()
            => Task.FromResult<IEnumerable<CacheItem>>(this.cache.Values);


        protected override Task OnTimerElapsed()
        {
            var now = DateTime.UtcNow;
            lock (this.syncLock)
            {
                var list = this.cache.Keys
                    .Select(x => cache[x])
                    .Where(x => x.ExpiryTime < now)
                    .ToList();

                foreach (var item in list)
                    this.cache.Remove(item.Key);
            }
            return Task.CompletedTask;
        }


        public override Task Clear()
        {
            lock (this.syncLock)
                this.cache.Clear();

            return Task.CompletedTask;
        }


        public override Task<T> Get<T>(string key)
        {
            if (!this.Enabled)
                return Task.FromResult(default(T));

            lock (this.syncLock)
            {
                if (!this.cache.ContainsKey(key))
                    return default;

                return Task.FromResult((T)this.cache[key].Object);
            }
        }


        public override Task<bool> Remove(string key)
        {
            if (!this.Enabled)
                return Task.FromResult(false);

            lock (this.syncLock)
                return Task.FromResult(this.cache.Remove(key));
        }


        public override Task Set(string key, object obj, TimeSpan? timeSpan = null)
        {
            if (!this.Enabled)
                return Task.CompletedTask;

            // I only need this call on set, since it doesn't have to clean until there is actually something there
            this.EnsureInitialized();
            lock (this.syncLock)
            {
                var ts = timeSpan ?? this.DefaultLifeSpan;
                var cacheObj = new CacheItem
                {
                    Key = key,
                    Object = obj,
                    ExpiryTime = DateTime.UtcNow.Add(ts)
                };
                this.cache[key] = cacheObj;
            }
            return Task.CompletedTask;
        }
    }
}
