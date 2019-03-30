using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Caching
{
    public class RepositoryCache : AbstractTimerCache
    {
        readonly IRepository repository;


        public RepositoryCache(IRepository repository,
                               TimeSpan? defaultLifeSpan = null,
                               TimeSpan? cleanUpTimer = null)
        {
            this.repository = repository;

            this.DefaultLifeSpan = defaultLifeSpan ?? TimeSpan.FromMinutes(10);
            this.CleanUpTime = cleanUpTimer ?? TimeSpan.FromSeconds(20);
        }


        public override Task Clear() => this.repository.Clear<CacheItem>();
        public override Task<bool> Remove(string key) => this.repository.Remove<CacheItem>(key);


        public override async Task<T> Get<T>(string key)
        {
            var cache = await this.repository.Get<CacheItem>(key);
            if (cache?.Object == null)
                return default;

            return (T)cache.Object;
        }


        public override async Task<IEnumerable<CacheItem>> GetCachedItems()
            => await this.repository.GetAll<CacheItem>();


        public override async Task Set(string key, object obj, TimeSpan? timeSpan = null)
        {
            var ts = timeSpan ?? this.DefaultLifeSpan;
            var expiry = DateTime.UtcNow.Add(ts);
            var cacheItem = new CacheItem
            {
                Key = key,
                Object = obj,
                ExpiryTime = expiry
            };
            await this.repository.Set(key, cacheItem);
        }


        protected override async Task OnTimerElapsed()
        {
            var now = DateTime.UtcNow;
            var items = await this.repository.GetAll<CacheItem>();
            foreach (var item in items)
                if (item.ExpiryTime < now)
                    await this.repository.Remove<CacheItem>(item.Key);
        }
    }
}
