using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Stores
{
    public class StoresModule : ShinyModule
    {
        public override void Register(IServiceCollection services)
        {
            services.AddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
            services.AddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();

            services.AddSingleton<IKeyValueStore, MemoryKeyValueStore>();
            services.AddSingleton<IKeyValueStore, FileKeyValueStore>();
#if !NETSTANDARD
            services.AddSingleton<IKeyValueStore, SettingsKeyValueStore>();
#if !WINDOWS_UWP
            services.AddSingleton<IKeyValueStore, SecureKeyValueStore>();
#endif
#endif
        }
    }
}
