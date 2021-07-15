using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Infrastructure;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Stores;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterCommonServices(this IServiceCollection services)
        {
#if !NETSTANDARD
            // stores
            services.AddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
            services.AddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
            services.AddSingleton<IKeyValueStore, SettingsKeyValueStore>();
            services.AddSingleton<IKeyValueStore, SecureKeyValueStore>();

            // do not register by default
            //services.AddSingleton<IKeyValueStore, MemoryKeyValueStore>();
            //services.AddSingleton<IKeyValueStore, FileKeyValueStore>();

            services.TryAddSingleton<StartupModule>();
            services.TryAddSingleton<ShinyCoreServices>();
            services.TryAddSingleton<ISerializer, ShinySerializer>();
            services.TryAddSingleton<IMessageBus, MessageBus>();
            services.TryAddSingleton<IRepository, FileSystemRepositoryImpl>();
            services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
            services.TryAddSingleton<IConnectivity, ConnectivityImpl>();

#if __IOS__
            services.TryAddSingleton<AppleLifecycle>();

            if (BgTasksJobManager.IsAvailable)
                services.TryAddSingleton<IJobManager, BgTasksJobManager>();
            else
                services.TryAddSingleton<IJobManager, JobManager>();
#else
            services.TryAddSingleton<IJobManager, JobManager>();
#endif

#endif
        }
    }
}