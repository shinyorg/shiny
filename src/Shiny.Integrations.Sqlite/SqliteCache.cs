using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shiny.Caching;

namespace Shiny
{
    class SqliteCache : ICache
    {
        public TimeSpan DefaultLifeSpan { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Enabled { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Task Clear()
        {
            throw new NotImplementedException();
        }

        public Task<T> Get<T>(string key)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<CacheItem>> GetCachedItems()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Remove(string key)
        {
            throw new NotImplementedException();
        }

        public Task Set(string key, object obj, TimeSpan? timeSpan = null)
        {
            throw new NotImplementedException();
        }

        public Task<T> TryGet<T>(string key, Func<Task<T>> getter, TimeSpan? timeSpan = null)
        {
            throw new NotImplementedException();
        }
    }
}
