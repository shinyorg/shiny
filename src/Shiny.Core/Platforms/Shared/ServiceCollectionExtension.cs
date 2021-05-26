using System;
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
            services.RegisterModule<StoresModule>();
            services.TryAddSingleton<StartupModule>();
            services.TryAddSingleton<ShinyCoreServices>();
            services.TryAddSingleton<ISerializer, ShinySerializer>();
            services.TryAddSingleton<IMessageBus, MessageBus>();
            services.TryAddSingleton<IRepository, FileSystemRepositoryImpl>();

            #if !TIZEN
            services.TryAddSingleton<IJobManager, JobManager>();
            #endif

            #if !NETSTANDARD
            services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
            #endif

            #if __TVOS__ || __WATCHOS__ || NETSTANDARD
            services.TryAddSingleton<IConnectivity, SharedConnectivityImpl>();
            #else
            services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
            #endif

#if __IOS__
            services.TryAddSingleton<AppleLifecycle>();

            if (BgTasksJobManager.IsAvailable)
                services.TryAddSingleton<IJobManager, BgTasksJobManager>();
            else
                services.TryAddSingleton<IJobManager, JobManager>();
#endif
        }
    }
}
