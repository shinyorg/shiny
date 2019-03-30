using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Caching
{
    public interface ICache
    {
        TimeSpan DefaultLifeSpan { get; set; }
        bool Enabled { get; set; }

        Task<IEnumerable<CacheItem>> GetCachedItems();
        Task Set(string key, object obj, TimeSpan? timeSpan = null);
        Task<T> Get<T>(string key);
        Task<T> TryGet<T>(string key, Func<Task<T>> getter, TimeSpan? timeSpan = null);
        Task<bool> Remove(string key);
        Task Clear();
    }
}
