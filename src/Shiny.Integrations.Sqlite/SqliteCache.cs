using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shiny.Caching;
using Shiny.Infrastructure;
using Shiny.Logging;
using Shiny.Models;


namespace Shiny
{
    class SqliteCache : AbstractTimerCache
    {
        readonly ShinySqliteConnection conn;
        readonly ISerializer serializer;


        public SqliteCache(ShinySqliteConnection conn, ISerializer serializer)
        {
            this.conn = conn;
            this.serializer = serializer;
        }


        public override Task Clear()
            => this.conn.DeleteAllAsync<CacheItem>();


        public override async Task<T> Get<T>(string key)
        {
            var item = await this.conn.GetAsync<CacheStore>(key);
            if (item == null)
                return default;

            var cache = this.serializer.Deserialize<T>(item.Blob);
            return cache;
        }


        public override async Task<IEnumerable<CacheItem>> GetCachedItems()
        {
            var items = await this.conn.Cache.ToListAsync();
            return items.Select(x => new CacheItem
            {
                Key = x.Key,
                Object = this.serializer.Deserialize(Type.GetType(x.TypeName), x.Blob),
                ExpiryTime = x.ExpiryDateUtc
            });
        }

        public override async Task<bool> Remove(string key)
        {
            var count = await this.conn.DeleteAsync<CacheStore>(key);
            return count > 0;
        }


        public override async Task Set(string key, object obj, TimeSpan? timeSpan = null)
        {
            var expiry = DateTime.UtcNow.Add(timeSpan ?? TimeSpan.FromMinutes(30));
            var blob = this.serializer.Serialize(obj);

            await this.conn.InsertOrReplaceAsync(new CacheStore
            {
                Key = key,
                Blob = blob,
                TypeName = obj.GetType().FullName,
                ExpiryDateUtc = expiry
            });
        }

        protected override async Task OnTimerElapsed()
        {
            try
            {
                await this.conn.ExecuteScalarAsync<int>("DELETE FROM CacheStore WHERE ExpiryDateUtc IS NOT NULL AND ExpiryDateUtc < ?", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
