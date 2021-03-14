//using System;
//using System.Collections.Generic;
//using System.Reactive.Linq;
//using System.Reactive.Subjects;
//using System.Text;
//using Windows.Security.Cryptography.DataProtection;
//using Windows.Storage;
//using Shiny.Infrastructure;


//namespace Shiny.Stores
//{
//    public class SecureKeyValueStore : IKeyValueStore
//    {
//        readonly Subject<KeyValuePair<string, object>> flushReq;
//        readonly SettingsKeyValueStore settingsStore;


//        public SecureKeyValueStore(ISerializer serializer)
//        {
//            this.settingsStore = new SettingsKeyValueStore(serializer) { ContainerName = "ShinySecure" };
//            this.flushReq = new Subject<KeyValuePair<string, object>>();

//            this.flushReq
//                .Throttle(TimeSpan.FromSeconds(500))
//                .Subscribe(_ =>
//                {
//                    var provider = new DataProtectionProvider("LOCAL=user");

//                    // iterate through ALL values?
//                    //var bytes = Encoding.UTF8.GetBytes(data);
//                    //provider.ProtectAsync()
//                });
//        }


//        public string Alias => "secure";
//        public void Clear() => this.settingsStore.Clear();
//        public bool Contains(string key) => this.settingsStore.Contains(key);
//        public object? Get(Type type, string key)
//        {
//            // ITERATE THROUGH ALL VALUES AND UNENCRYPT to mem dictionary?
//            //var data = this.settingsStore.Get<byte[]>(key);
//            //if (data == null)
//            //    return null;

//            //var provider = new DataProtectionProvider();
//            //var buffer = await provider.UnprotectAsync(data);
//            //var value = Encoding.UTF8.GetString(buffer.ToArray());
//            return null;
//        }
//        public bool Remove(string key) => this.settingsStore.Remove(key);
//        public void Set(string key, object value) => this.DoSet(key, value);


//        void DoSet(string key, object value)
//        {


//            //Encoding.UTF8.GetString(buffer.ToArray());

//        }

//        //static async Task<string> PlatformGetAsync(string key)
//        //{
//        //    var settings = GetSettings(Alias);

//        //    var encBytes = settings.Values[key] as byte[];

//        //    if (encBytes == null)
//        //        return null;

//        //    var provider = new DataProtectionProvider();

//        //    var buffer = await provider.UnprotectAsync(encBytes.AsBuffer());

//        //    return Encoding.UTF8.GetString(buffer.ToArray());
//        //}

//        //static async Task PlatformSetAsync(string key, string data)
//        //{
//        //    var settings = GetSettings(Alias);

//        //    var bytes = Encoding.UTF8.GetBytes(data);

//        //    // LOCAL=user and LOCAL=machine do not require enterprise auth capability
//        //    var provider = new DataProtectionProvider("LOCAL=user");

//        //    var buffer = await provider.ProtectAsync(bytes.AsBuffer());

//        //    var encBytes = buffer.ToArray();

//        //    settings.Values[key] = encBytes;
//        //}

//        //static bool PlatformRemove(string key)
//        //{
//        //    var settings = GetSettings(Alias);

//        //    if (settings.Values.ContainsKey(key))
//        //    {
//        //        settings.Values.Remove(key);
//        //        return true;
//        //    }

//        //    return false;
//        //}

//        //static void PlatformRemoveAll()
//        //{
//        //    var settings = GetSettings(Alias);

//        //    settings.Values.Clear();
//        //}
//        static ApplicationDataContainer GetSettings(string name)
//        {
//            var ls = ApplicationData.Current.LocalSettings;

//            if (!ls.Containers.ContainsKey(name))
//                ls.CreateContainer(name, ApplicationDataCreateDisposition.Always);

//            return ls.Containers[name];
//        }

//    }
//}
