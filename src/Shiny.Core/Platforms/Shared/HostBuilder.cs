using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Logging;

namespace Shiny.Hosting;


public abstract class HostBuilder : IHostBuilder
{

    protected HostBuilder()
    {
        this.Services = new ServiceCollection();
        //this.Lifecycle = new LifecycleBuilder();
        this.Logging = new ShinyLoggingBuilder(this.Services);
        this.ConfigureContainer = s => s.BuildServiceProvider();
    }


    public IServiceCollection Services { get; }
    public ILifecycleBuilder Lifecycle { get; }
    public ILoggingBuilder Logging { get; }
    public Func<IServiceCollection, IServiceProvider> ConfigureContainer { get; set; }


    protected virtual void PreBuildPlatformHost() { }
    protected abstract IHost BuildPlatformHost(ILoggerFactory loggerFactory, IServiceProvider serviceProvider);


    public virtual IHost Build()
    {
        //var lifecycle = this.Lifecycle.Build();
        //this.Services.AddSingleton(lifecycle);
        this.PreBuildPlatformHost();
        var serviceProvider = this.ConfigureContainer(this.Services);
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        this.Services.AddSingleton<ILoggerFactory, LoggerFactory>();
        this.Services.AddSingleton(typeof(ILogger<>), typeof(GenericLogger<>));

        var host = this.BuildPlatformHost(loggerFactory, serviceProvider);
        Host.Current = host;
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