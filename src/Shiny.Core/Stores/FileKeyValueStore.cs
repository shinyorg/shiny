using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.Infrastructure;


namespace Shiny.Stores
{
    public class FileKeyValueStore : IKeyValueStore
    {
        readonly ISerializer serializer;
        readonly Subject<object> flushReq;
        readonly string filePath;


        public FileKeyValueStore(IPlatform platform, ISerializer serializer, string? fileName = null)
        {
            this.serializer = serializer;
            this.FileName = fileName ?? "shinystore";
            this.filePath = Path.Combine(platform.AppData.FullName, this.FileName);

            this.flushReq = new Subject<object>();
            this.flushReq
                .Throttle(TimeSpan.FromSeconds(500))
                .Subscribe(_ => this.Do(values =>
                {
                    var contents = this.serializer.Serialize(values);
                    File.WriteAllText(this.filePath, contents);
                }, false));
        }


        public string Alias => "file";
        public string FileName { get; }


        public bool Contains(string key) => this.Do(x => x.ContainsKey(key), false);
        public T? Get<T>(string key) => (T?)this.Get(typeof(T), key);
        public object? Get(Type type, string key) => this.Do(values =>
            values.ContainsKey(key) ? values[key] : null
        , false);


        public bool Remove(string key) => this.Do(values => values.Remove(key), true);
        public void Clear() => this.Do(x => x.Clear(), true);
        public void Set(string key, object value) => this.Do(x => x[key] = value, true);


        readonly object syncLock = new object();

        Dictionary<string, object>? keyValues;
        protected virtual void Do(Action<Dictionary<string, object>> worker, bool requestFlush) => this.Do<object>(values =>
        {
            worker(values);
            return null;
        }, false);
        protected virtual T Do<T>(Func<Dictionary<string, object>, T?> worker, bool requestFlush)
        {
            lock (this.syncLock)
            {
                if (this.keyValues == null)
                {
                    if (!File.Exists(this.filePath))
                    {
                        this.keyValues = new Dictionary<string, object>();
                    }
                    else
                    {
                        var contents = File.ReadAllText(this.filePath);
                        this.keyValues = this.serializer.Deserialize<Dictionary<string, object>>(contents) ?? new Dictionary<string, object>();
                    }
                }
                var result = worker(this.keyValues);
                if (requestFlush)
                    this.flushReq.OnNext(null);

                return result;
            }
        }
    }
}
