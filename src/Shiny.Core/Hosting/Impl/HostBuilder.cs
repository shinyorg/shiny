using System;
//using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Hosting.Impl;


public class HostBuilder : IHostBuilder
{
    //readonly IConfigurationBuilder configBuilder;
    
    
    public HostBuilder(IPlatform platform)
    {
        // TODO: default configuration?
        this.Platform = platform;
        this.Services = new ServiceCollection();
        this.Lifecycle = new LifecycleBuilder();
        //this.Configuration = new ConfigurationManager();
    }


    public IPlatform Platform { get; }
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

        this.Services.AddSingleton(this.Platform);
        //this.Services.AddSingleton(this.Configuration);
        this.Services.AddSingleton(_ => host!);
        
        var lifecycle = this.Lifecycle.Build();
        this.Services.AddSingleton(lifecycle);
        
        var serviceProvider = this.Services.BuildServiceProvider();
        host = new Host 
        {
            ServiceProvider = serviceProvider,
            //Configuration = this.Configuration,
            Platform = this.Platform,
            Logging = serviceProvider.GetRequiredService<ILoggerFactory>()
        };
        
        // TODO: push serviceprovider to static somewhere
            // TODO: maui and blazor won't use this
        return host;
    }
}
