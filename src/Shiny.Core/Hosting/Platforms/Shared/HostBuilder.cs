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
        //this.Lifecycle = new LifecycleBuilder();
        //new ShinyLoggingBuilder(this.Services);
    }


    public IServiceCollection Services { get; }
    //public ConfigurationManager Configuration { get; private set; }
    public ILifecycleBuilder Lifecycle { get; }


    public IHostBuilder ConfigureLogging(Action<ILoggingBuilder> builder)
    {
        this.Services.AddLogging(builder);
        return this;
    }


    public virtual IHost Build()
    {
        IHost? host = null;

        //this.Services.AddSingleton(this.Configuration);
        this.Services.AddSingleton(_ => host!);
        

        //var lifecycle = this.Lifecycle.Build();
        //this.Services.AddSingleton(lifecycle);

        var serviceProvider = this.Services.BuildServiceProvider();
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