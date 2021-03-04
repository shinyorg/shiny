using System;
using Foundation;
using Security;
using Shiny.Infrastructure;


namespace Shiny.Stores
{
    public class SecureKeyValueStore : IKeyValueStore
    {
        readonly ISerializer serializer;
        public SecAccessible DefaultAccessible { get; set; } = SecAccessible.Always;
        public string Alias { get; set; }


        public SecureKeyValueStore(IPlatform platform, ISerializer serializer)
        {
            this.Alias = $"{platform.AppIdentifier}.shiny";
            this.serializer = serializer;
        }


        public void Clear()
        {
            using (var query = new SecRecord(SecKind.GenericPassword) { Service = this.Alias })
                SecKeyChain.Remove(query);
        }


        public bool Contains(string key)
        {
            using (var record = this.GetRecord(key))
                using (var match = SecKeyChain.QueryAsRecord(record, out var result))
                    return result == SecStatusCode.Success;
        }


        public T? Get<T>(string key) => (T?)this.Get(typeof(T), key);
        public object? Get(Type type, string key)
        {
            object? result = null;
            using (var record = this.GetRecord(key))
            {
                using (var match = SecKeyChain.QueryAsRecord(record, out var resultCode))
                {
                    if (resultCode == SecStatusCode.Success)
                    {
                        var value = NSString.FromData(match.ValueData, NSStringEncoding.UTF8);
                        result = this.serializer.Deserialize(type, value);
                    }
                }
            }
            return result;
        }


        public bool Remove(string key)
        {
            var removed = false;
            using (var record = this.GetRecord(key))
            {
                using (var match = SecKeyChain.QueryAsRecord(record, out var result))
                {
                    if (result == SecStatusCode.Success)
                    {
                        result = SecKeyChain.Remove(record);
                        if (result != SecStatusCode.Success)
                            throw new ArgumentException("Error removing secure value - " + result);

                        removed = true;
                    }
                }
            }
            return removed;
        }


        public void Set<T>(string key, T value) => this.DoSet(key, value);
        public void Set(string key, object value) => this.DoSet(key, value);


        protected virtual void DoSet(string key, object value)
        {
            this.Remove(key);

            var content = this.serializer.Serialize(value);
            var record = new SecRecord(SecKind.GenericPassword)
            {
                Account = key,
                Service = this.Alias,
                Label = key,
                Accessible = DefaultAccessible,
                ValueData = NSData.FromString(content, NSStringEncoding.UTF8),
            };
            var result = SecKeyChain.Add(record);

            if (result != SecStatusCode.Success)
                throw new ArgumentException("Failed to add secure value - " + result);
        }


        protected virtual SecRecord GetRecord(string key) => new SecRecord(SecKind.GenericPassword)
        {
            Account = key,
            Service = this.Alias
        };
    }
}