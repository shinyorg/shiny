using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Logging;

namespace Shiny.Hosting;


public class HostBuilder : IHostBuilder
{

    public HostBuilder()
    {
        this.Services = new ServiceCollection();
        this.Lifecycle = new LifecycleBuilder();
        this.Logging = new ShinyLoggingBuilder(this.Services);

#if __ANDROID__
#elif __IOS__
#endif
    }


    public IServiceCollection Services { get; }
    public ILifecycleBuilder Lifecycle { get; }
    public ILoggingBuilder Logging { get; }


    public virtual IHost Build()
    {
        IHost? host = null;
        //var lifecycle = this.Lifecycle.Build();
        //this.Services.AddSingleton(lifecycle);

        this.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
        this.Services.AddSingleton(typeof(ILogger<>), typeof(GenericLogger<>));
        var serviceProvider = this.Services.BuildServiceProvider();

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
#if __ANDROID__
        //new AndroidHost(Application.Context)
#elif __IOS__
        var host = new IosHost();

#else
#endif
        //host = new Host
        //{
        //    ServiceProvider = serviceProvider,
        //    //Configuration = this.Configuration,
        //    Platform = this.Platform,
        //    Logging = serviceProvider.GetRequiredService<ILoggerFactory>()
        //};

        // TODO: push serviceprovider to static somewhere
        // TODO: maui and blazor won't use this
        //Host.Current = host;
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

//            services.TryAddSingleton<StartupModule>();
//            services.TryAddSingleton<ShinyCoreServices>();
//            services.TryAddSingleton<ISerializer, ShinySerializer>();
//            services.TryAddSingleton<IMessageBus, MessageBus>();
//            services.TryAddSingleton<IRepository, FileSystemRepositoryImpl>();
//            services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
//            services.TryAddSingleton<IConnectivity, ConnectivityImpl>();