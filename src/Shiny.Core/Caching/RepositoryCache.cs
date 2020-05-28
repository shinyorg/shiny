using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Caching
{
    public class RepositoryCache : AbstractTimerCache
    {
        readonly IRepository repository;
        readonly ISerializer serializer;

        public RepositoryCache(IRepository repository, ISerializer serializer,
                               TimeSpan? defaultLifeSpan = null,
                               TimeSpan? cleanUpTimer = null)
        {
            this.repository = repository;
            this.serializer = serializer;

            this.DefaultLifeSpan = defaultLifeSpan ?? TimeSpan.FromMinutes(10);
            this.CleanUpTime = cleanUpTimer ?? TimeSpan.FromSeconds(20);
        }


        public override Task Clear() => this.repository.Clear<CacheItem>();
        public override Task<bool> Remove(string key) => this.repository.Remove<CacheItem>(key);


        public override async Task<T> Get<T>(string key)
        {
            var item = await this.repository.Get<CacheItem>(key);
            if (item?.Object == null)
                return default;

            if (!(item.Object is T cache))
                cache = this.serializer.Deserialize<T>(item.Object.ToString());

            return cache;
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
