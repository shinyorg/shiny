using System;
using System.Threading.Tasks;


namespace Shiny.Caching
{
    public interface ICache
    {
        TimeSpan DefaultLifeSpan { get; set; }
        bool Enabled { get; set; }
        void Set(string key, object obj, TimeSpan? timeSpan = null);
        T Get<T>(string key);
        Task<T> TryGet<T>(string key, Func<Task<T>> getter, TimeSpan? timeSpan = null);
        bool Remove(string key);
        void Clear();
    }
}
