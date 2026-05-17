using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Security.Cryptography.DataProtection;

namespace Shiny.Stores;


public class SecureKeyValueStore(ISerializer serializer) : IKeyValueStore
{
    readonly SettingsKeyValueStore settingsStore = new(serializer) { ContainerName = "ShinySecure" };


    public bool IsReadOnly => false;
    public void Clear() => this.settingsStore.Clear();
    public bool Contains(string key) => this.settingsStore.Contains(key);
    public bool Remove(string key) => this.settingsStore.Remove(key);


    public T? Get<T>(string key)
    {
        var data = this.settingsStore.Get<byte[]>(key);
        if (data == null)
            return default;

        var provider = new DataProtectionProvider();
        var buffer = provider.UnprotectAsync(data.AsBuffer()).AsTask().GetAwaiter().GetResult();
        var json = Encoding.UTF8.GetString(buffer.ToArray());
        return serializer.Deserialize<T>(json);
    }


    public void Set<T>(string key, T value)
    {
        var json = serializer.Serialize<T>(value);
        var bytes = Encoding.UTF8.GetBytes(json);

        // LOCAL=user and LOCAL=machine do not require enterprise auth capability
        var provider = new DataProtectionProvider("LOCAL=user");
        var buffer = provider.ProtectAsync(bytes.AsBuffer()).AsTask().GetAwaiter().GetResult();
        this.settingsStore.Set(key, buffer.ToArray());
    }
}
