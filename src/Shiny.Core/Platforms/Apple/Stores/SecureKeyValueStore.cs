using Foundation;
using Security;

namespace Shiny.Stores;


public class SecureKeyValueStore(IosPlatform platform, ISerializer serializer) : IKeyValueStore
{
    readonly object syncLock = new();
    public SecAccessible DefaultAccessible { get; set; } = SecAccessible.Always;
    public bool IsReadOnly => false;
    public string Service { get; set; } = $"{platform.AppIdentifier}.secure";


    public void Clear()
    {
        lock (this.syncLock)
        {
            using var query = new SecRecord(SecKind.GenericPassword) { Service = this.Service };
            SecKeyChain.Remove(query);
        }
    }


    public bool Contains(string key)
    {
        lock (this.syncLock)
        {
            using var record = this.GetRecord(key);
            using var match = SecKeyChain.QueryAsRecord(record, out var result);
            return result == SecStatusCode.Success;
        }
    }


    public T? Get<T>(string key)
    {
        lock (this.syncLock)
        {
            using var record = this.GetRecord(key);
            using var match = SecKeyChain.QueryAsRecord(record, out var resultCode);

            if (resultCode != SecStatusCode.Success)
                return default;

            var value = NSString.FromData(match!.ValueData!, NSStringEncoding.UTF8)!;
            return serializer.Deserialize<T>(value);
        }
    }


    public bool Remove(string key)
    {
        lock (this.syncLock)
        {
            using var record = this.GetRecord(key);
            using var match = SecKeyChain.QueryAsRecord(record, out var result);
            if (result != SecStatusCode.Success)
                return false;

            result = SecKeyChain.Remove(record);
            if (result != SecStatusCode.Success)
                throw new System.ArgumentException("Error removing secure value - " + result);

            return true;
        }
    }


    public void Set<T>(string key, T value)
    {
        this.Remove(key);

        lock (this.syncLock)
        {
            var content = serializer.Serialize<T>(value);
            var record = new SecRecord(SecKind.GenericPassword)
            {
                Account = key,
                Service = this.Service,
                Label = key,
                Accessible = this.DefaultAccessible,
                ValueData = NSData.FromString(content, NSStringEncoding.UTF8),
            };
            var result = SecKeyChain.Add(record);

            if (result != SecStatusCode.Success)
                throw new System.ArgumentException("Failed to add secure value - " + result);
        }
    }


    protected virtual SecRecord GetRecord(string key) => new SecRecord(SecKind.GenericPassword)
    {
        Account = key,
        Service = this.Service
    };
}
