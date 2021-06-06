using System;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Linq;
using Windows.Security.Cryptography.DataProtection;
using Shiny.Infrastructure;


namespace Shiny.Stores
{
    public class SecureKeyValueStore : IKeyValueStore
    {
        readonly SettingsKeyValueStore settingsStore;
        readonly ISerializer serializer;


        public SecureKeyValueStore(ISerializer serializer)
        {
            this.serializer = serializer;
            this.settingsStore = new SettingsKeyValueStore(serializer) { ContainerName = "ShinySecure" };
        }


        public string Alias => "secure";
        public bool IsReadOnly => false;
        public void Clear() => this.settingsStore.Clear();
        public bool Contains(string key) => this.settingsStore.Contains(key);
        public object? Get(Type type, string key)
        {
            // ITERATE THROUGH ALL VALUES AND UNENCRYPT to mem dictionary?
            var data = this.settingsStore.Get<byte[]>(key);
            if (data == null)
                return null;

            var provider = new DataProtectionProvider();
            var buffer = provider.UnprotectAsync(data.AsBuffer()).GetResults();
            var value = Encoding.UTF8.GetString(buffer.ToArray());
            return value;
        }
        public bool Remove(string key) => this.settingsStore.Remove(key);
        public void Set(string key, object value) => this.DoSet(key, value);


        void DoSet(string key, object value)
        {
            var data = this.serializer.Serialize(value);
            var bytes = Encoding.UTF8.GetBytes(data);

            // LOCAL=user and LOCAL=machine do not require enterprise auth capability
            var provider = new DataProtectionProvider("LOCAL=user");
            var buffer = provider.ProtectAsync(bytes.AsBuffer()).GetResults();
            this.settingsStore.Set(key, buffer.ToArray());
        }
    }
}
