using System;
using Javax.Crypto;
using Microsoft.Extensions.Logging;

namespace Shiny.Stores;


public class SecureKeyValueStore : IKeyValueStore
{
    readonly object syncLock = new();
    readonly SettingsKeyValueStore settingsStore;
    readonly AndroidKeyStore keyStore;
    readonly ISerializer serializer;


    public SecureKeyValueStore(
        ILogger<SecureKeyValueStore> logger,
        AndroidPlatform platform,
        ISerializer serializer
    )
    {
        this.settingsStore = new SettingsKeyValueStore(platform, serializer);
        this.serializer = serializer;

        this.keyStore = new AndroidKeyStore(
            platform.AppContext,
            this.settingsStore,
            logger,
            $"{platform.AppContext.PackageName}.secure",
            false
        );
    }


    public bool IsReadOnly => false;


    public void Clear() => this.settingsStore.Clear();
    public bool Contains(string key) => this.settingsStore.Contains(SecureKey(key));
    public bool Remove(string key) => this.settingsStore.Remove(SecureKey(key));


    public T? Get<T>(string key)
    {
        var secureKey = SecureKey(key);
        if (!this.settingsStore.Contains(secureKey))
            return default;

        var encValue = this.settingsStore.Get<string>(secureKey);
        if (encValue == null)
            return default;

        var data = Convert.FromBase64String(encValue);
        lock (this.syncLock)
        {
            try
            {
                var value = this.keyStore.Decrypt(data);
                return this.serializer.Deserialize<T>(value);
            }
            catch (AEADBadTagException)
            {
                // unable to decrypt due to app uninstall; remove stale key
                this.Remove(key);
                return default;
            }
        }
    }


    public void Set<T>(string key, T value)
    {
        var content = this.serializer.Serialize<T>(value);
        var data = this.keyStore.Encrypt(content);
        var encValue = Convert.ToBase64String(data);
        this.settingsStore.Set(SecureKey(key), encValue);
    }


    static string SecureKey(string key) => "sec-" + key;
}
