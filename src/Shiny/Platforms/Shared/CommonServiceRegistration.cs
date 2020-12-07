using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Infrastructure;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;


namespace Shiny
{
    public static class CommonServiceRegistration
    {
        public static void RegisterCommonServices(this IServiceCollection services)
        {
            services.AddSingleton<ShinyCoreServices>();
            services.TryAddSingleton<ISerializer, ShinySerializer>();
            services.TryAddSingleton<IMessageBus, MessageBus>();
            services.TryAddSingleton<IRepository, FileSystemRepositoryImpl>();

            #if !TIZEN
            services.TryAddSingleton<IJobManager, JobManager>();
            #endif

            #if !NETSTANDARD
            services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
            services.TryAddSingleton<ISettings, SettingsImpl>();
            #endif

            #if __TVOS__ || __WATCHOS__ || NETSTANDARD
            services.TryAddSingleton<IConnectivity, SharedConnectivityImpl>();
            #else
            services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
            #endif
        }
    }
}
