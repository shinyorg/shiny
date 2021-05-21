using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Shiny.Infrastructure;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Stores;


namespace Shiny
{
    public static class CommonServiceRegistration
    {
        public static void RegisterCommonServices(this IServiceCollection services)
        {
            //.UseDefaultServiceProvider((context, options) =>
            // {
            //     bool isDevelopment = context.HostingEnvironment.IsDevelopment();
            //     options.ValidateScopes = isDevelopment;
            //     options.ValidateOnBuild = isDevelopment;
            // });
            services
                .AddOptions()
                .ConfigureOptions<ConfigureShinyOptions>()
                .Configure<ShinyOptions>(opts => {
                    opts.Services = services;
                });

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
        }
    }
}
