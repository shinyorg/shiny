using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Shiny.Logging;
#if PLATFORM
using Shiny.Infrastructure;
#endif

namespace Shiny;


public class HostBuilder : IHostBuilder
{
    public HostBuilder()
    {
        this.Services = new ServiceCollection();
        this.Logging = new ShinyLoggingBuilder(this.Services);
        this.Configuration = new ConfigurationManager();
    }


    public static HostBuilder Create() => new HostBuilder();

    public Func<IServiceCollection, IServiceProvider>? ConfigureContainer { get; set; }
    public IServiceCollection Services { get; }
    public ILoggingBuilder Logging { get; }
    public ConfigurationManager Configuration { get; }


    public IHost Build()
    {
#if PLATFORM
        this.Services.AddShinyCoreServices();
#endif
        this.Services.AddSingleton<IConfiguration>(this.Configuration);

        var serviceProvider = this.ConfigureContainer == null
            ? this.Services.BuildServiceProvider()
            : this.ConfigureContainer.Invoke(this.Services);

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        return new Host(serviceProvider, loggerFactory);
    }
}