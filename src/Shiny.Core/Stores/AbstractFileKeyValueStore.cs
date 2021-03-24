using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.Infrastructure;


namespace Shiny.Stores
{
    public abstract class AbstractFileKeyValueStore : IKeyValueStore
    {
        readonly Subject<object> flushReq;
        readonly IPlatform platform;


        protected AbstractFileKeyValueStore(IPlatform platform, string alias, bool readOnly)
        {
            this.platform = platform;
            this.Alias = alias;
            this.IsReadOnly = readOnly;
            this.flushReq = new Subject<object>();

            this.flushReq
                .Throttle(TimeSpan.FromSeconds(500))
                .Subscribe(_ => this.Store(this.path!, this.values!));
        }


        public string FileName { get; set; }
        protected abstract IDictionary<string, object> Load(string path);
        protected virtual void Store(string path, IDictionary<string, object> values)
            => throw new NotImplementedException();


        public string Alias { get; }
        public bool IsReadOnly { get; }

        public bool Contains(string key) => this.GetValues().ContainsKey(key);
        public object? Get(Type type, string key)
        {
            var values = this.GetValues();
            if (!values.ContainsKey(key))
                return type.GetDefaultValue();

            return values[key];
        }


        public void Clear()
            => this.DoStore(values => values.Clear());


        public bool Remove(string key)
        {
            var removed = false;
            this.DoStore(values => removed = values.Remove(key));
            return removed;
        }


        public void Set(string key, object value)
            => this.DoStore(values => values[key] = value);


        protected void DoStore(Action<IDictionary<string, object>> process)
        {
            if (this.IsReadOnly)
                throw new NotImplementedException($"This store '{this.Alias}' is read-only");

            var values = this.GetValues();
            lock (this.syncLock)
                process(values);

            this.flushReq.OnNext(null);
        }


        string? path;
        readonly object syncLock = new object();
        IDictionary<string, object>? values;
        protected IDictionary<string, object> GetValues()
        {
            if (this.values == null)
            {
                lock (this.syncLock)
                {
                    if (this.values == null)
                    {
                        this.path = Path.Combine(this.platform.AppData.FullName, this.FileName);
                        this.values = File.Exists(this.path)
                            ? this.Load(path)
                            : new Dictionary<string, object>();
                    }
                }
            }
            return this.values;
        }
    }
}
