using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Caching;


namespace Shiny
{
    class SqliteCache : AbstractTimerCache
    {
        readonly ShinySqliteConnection conn;
        public SqliteCache(ShinySqliteConnection conn)
            => this.conn = conn;


        public override Task Clear()
        {
            throw new NotImplementedException();
        }

        public override Task<T> Get<T>(string key)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<CacheItem>> GetCachedItems()
        {
            throw new NotImplementedException();
        }

        public override Task<bool> Remove(string key)
        {
            throw new NotImplementedException();
        }

        public override Task Set(string key, object obj, TimeSpan? timeSpan = null)
        {
            throw new NotImplementedException();
        }

        protected override Task OnTimerElapsed()
        {
            throw new NotImplementedException();
        }
    }
}
