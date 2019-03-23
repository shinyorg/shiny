using System;
using System.Threading.Tasks;


namespace Shiny.Caching
{
    public abstract class AbstractCache : ICache, IDisposable
    {
        readonly object syncLock = new object();
        bool init;

        protected abstract void Init();


        protected void EnsureInitialized()
        {
            if (this.init)
                return;

            lock (this.syncLock)
            {
                this.Init();
                this.init = true;
            }
        }


        public virtual async Task<T> TryGet<T>(string key, Func<Task<T>> getter, TimeSpan? timeSpan = null)
        {
            var obj = this.Get<T>(key);
            if (obj == null)
            {
                obj = await getter();
                this.Set(key, obj, timeSpan);
            }
            return obj;
        }


        protected virtual void Dispose(bool disposing)
        {
        }


        public virtual void Dispose() => this.Dispose(true);

        public TimeSpan DefaultLifeSpan { get; set; }
        public bool Enabled { get; set; }

        public abstract void Set(string key, object obj, TimeSpan? timeSpan = null);
        public abstract T Get<T>(string key);
        public abstract bool Remove(string key);
        public abstract void Clear();
    }
}
