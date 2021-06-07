using System;
using Shiny.Infrastructure;
using Windows.Storage;


namespace Shiny.Stores
{
    public class SettingsKeyValueStore : IKeyValueStore
    {
        public string? ContainerName { get; set; }
        readonly ISerializer serializer;


        public SettingsKeyValueStore(ISerializer serializer)
            => this.serializer = serializer;


        public string Alias => "settings";
        public bool IsReadOnly => false;
        public void Clear() => this.Container.Values.Clear();
        public bool Contains(string key) => this.Container.Values.ContainsKey(key);
        public object? Get(Type type, string key)
        {
            if (!this.Contains(key))
                return null;

            var value = (string)this.container.Values[key];
            var obj = this.serializer.Deserialize(type, value);
            return obj;
        }
        public bool Remove(string key) => this.Container.Values.Remove(key);
        public void Set(string key, object value)
        {
            var s = this.serializer.Serialize(value);
            if (this.Contains(key))
                this.Container.Values[key] = s;
            else
                this.Container.Values.Add(key, s);
        }


        ApplicationDataContainer? container;
        protected virtual ApplicationDataContainer Container
        {
            get
            {
                this.ContainerName ??= "shiny";
                this.container ??= ApplicationData.Current.LocalSettings.CreateContainer(this.ContainerName, ApplicationDataCreateDisposition.Always);
                return this.container;
            }
        }
    }
}