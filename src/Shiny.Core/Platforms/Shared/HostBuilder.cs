﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Logging;

namespace Shiny.Hosting;


public abstract class HostBuilder : IHostBuilder
{

    protected HostBuilder()
    {
        this.Services = new ServiceCollection();
        this.Logging = new ShinyLoggingBuilder(this.Services);
        this.ConfigureContainer = s => s.BuildServiceProvider();
    }


    public IServiceCollection Services { get; }
    public ILoggingBuilder Logging { get; }
    public Func<IServiceCollection, IServiceProvider> ConfigureContainer { get; set; }



    public virtual IHost Build()
    {
        this.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
        this.Services.AddSingleton(typeof(ILogger<>), typeof(GenericLogger<>));

#if ANDROID
        this.AddAndroid();
#elif IOS || MACCATALYST
        this.AddIos();
#endif

        var serviceProvider = this.ConfigureContainer(this.Services);
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var host = new Host(serviceProvider, loggerFactory);

        return host;
    }
}
//services.AddSingleton<IObjectStoreBinder, ObjectStoreBinder>();
//            services.AddSingleton<IKeyValueStoreFactory, KeyValueStoreFactory>();
//            services.AddSingleton<IKeyValueStore, SettingsKeyValueStore>();
//            services.AddSingleton<IKeyValueStore, SecureKeyValueStore>();

//            // do not register by default
//            //services.AddSingleton<IKeyValueStore, MemoryKeyValueStore>();
//            //services.AddSingleton<IKeyValueStore, FileKeyValueStore>();
//            services.TryAddSingleton<IMessageBus, MessageBus>();
//            services.TryAddSingleton<IRepository, FileSystemRepositoryImpl>();
//            services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
//            services.TryAddSingleton<IConnectivity, ConnectivityImpl>();