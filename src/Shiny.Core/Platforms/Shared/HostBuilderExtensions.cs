using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Shiny.Infrastructure;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Stores;


namespace Shiny
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddShiny(this IHostBuilder hostBuilder)
            => hostBuilder
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<ShinyHostedService>();
#if MONOANDROID
                    //services.AddSingleton<IAndroidContext>(this);
#endif
                    services.RegisterCommonServices();
                });


        public static void RegisterCommonServices(this IServiceCollection services)
        {
            services.Configure<ShinyOptions>(x => x.Services = services);

            services.AddSingleton<StartupModule>();
            services.AddSingleton<ShinyCoreServices>();
            services.RegisterModule<StoresModule>();
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

            services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
            if (BgTasksJobManager.IsAvailable)
                services.TryAddSingleton<IJobManager, BgTasksJobManager>();
            else
                services.TryAddSingleton<IJobManager, JobManager>();
#endif
        }
    }
}
