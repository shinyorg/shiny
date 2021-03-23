using System;
using Javax.Crypto;
using Shiny.Infrastructure;


namespace Shiny.Stores
{
    public class SecureKeyValueStore : IKeyValueStore
    {
        readonly object syncLock = new object();
        readonly SettingsKeyValueStore settingsStore;
        readonly AndroidKeyStore keyStore;
        readonly ISerializer serializer;


        public SecureKeyValueStore(IPlatform platform,
                                   IAndroidContext context,
                                   ISerializer serializer)
        {

            this.settingsStore = new SettingsKeyValueStore(context, serializer);
            this.keyStore = new AndroidKeyStore(context, this.settingsStore, $"{platform.AppIdentifier}.secure", false);
            this.serializer = serializer;
        }


        public string Alias => "secure";
        public bool IsReadOnly => false;


        public void Clear()
        {
            //this.settingsStore.ToList().Where(x => x.Key.StartsWith("sec-").Clear(); // TODO: only clear secure storage
            this.settingsStore.Clear();
        }
        public bool Contains(string key) => this.settingsStore.Contains(SecureKey(key));
        public object? Get(Type type, string key)
        {
            var result = type.GetDefaultValue();
            var secureKey = SecureKey(key);

            if (this.settingsStore.Contains(secureKey))
            {
                var encValue = this.settingsStore.Get<string>(secureKey);
                var data = Convert.FromBase64String(encValue);
                lock (this.syncLock)
                {
                    try
                    {
                        var value = this.keyStore.Decrypt(data);
                        result = this.serializer.Deserialize(type, value);
                    }
                    catch (AEADBadTagException)
                    {
                        // unable to decrypt due to app uninstall, removing old key
                        this.Remove(key);
                    }
                }
            }
            return result;
        }

        public bool Remove(string key) => this.settingsStore.Remove(SecureKey(key));
        public void Set(string key, object value)
        {
            var content = this.serializer.Serialize(value);
            var data = this.keyStore.Encrypt(content);
            var encValue = Convert.ToBase64String(data);
            var secureKey = SecureKey(key);
            this.settingsStore.Set(secureKey, encValue);
        }

        static string SecureKey(string key) => "sec-" + key;
    }
}
