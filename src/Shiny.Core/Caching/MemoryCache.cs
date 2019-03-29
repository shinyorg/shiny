using System;
using System.Collections.Generic;
using System.Linq;


namespace Shiny.Caching
{
    public class MemoryCache : AbstractTimerCache
    {
        readonly IDictionary<string, object> cache = new Dictionary<string, object>();
        readonly object syncLock = new object();


        public MemoryCache(TimeSpan? defaultLifeSpan = null, TimeSpan? cleanUpTimer = null)
        {
            this.DefaultLifeSpan = defaultLifeSpan ?? TimeSpan.FromMinutes(10);
            this.CleanUpTime = cleanUpTimer ?? TimeSpan.FromSeconds(20);
        }


        protected override void OnTimerElapsed()
        {
            var now = DateTime.UtcNow;
            lock (this.syncLock)
            {
                var list = this.cache.Keys
                    .Select(x => (CacheItem)cache[x])
                    .Where(x => x.ExpiryTime < now)
                    .ToList();

                foreach (var item in list)
                    this.cache.Remove(item.Key);
            }
        }


        public override void Clear()
        {
            lock (this.syncLock)
                this.cache.Clear();
        }


        public override T Get<T>(string key)
        {
            if (!this.Enabled)
                return default;

            lock (this.syncLock)
            {
                if (!this.cache.ContainsKey(key))
                    return default;

                var item = (CacheItem)this.cache[key];
                return (T)item.Object;
            }
        }


        public override bool Remove(string key)
        {
            if (!this.Enabled)
                return false;

            lock (this.syncLock)
                return this.cache.Remove(key);
        }


        public override void Set(string key, object obj, TimeSpan? timeSpan = null)
        {
            if (!this.Enabled)
                return;

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
        }
    }
}
