using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Shiny.Logging;
#if PLATFORM
using Shiny.Infrastructure;
#endif

namespace Shiny;


public class HostBuilder
{
    HostBuilder(IServiceCollection? services, ILoggingBuilder? loggingBuilder)
    {
        this.Services = services ?? new ServiceCollection();
        this.Logging = loggingBuilder ?? new ShinyLoggingBuilder(this.Services);
        // this.Configuration = new ConfigurationManager();
    }


    /// <summary>
    /// Create a shiny host builder
    /// </summary>
    /// <param name="services">Pass ONLY if you have an existing service collection you want to build on</param>
    /// <param name="loggingBuilder">Pass ONLY if you have an existing logging builder you want to build on</param>
    /// <returns></returns>
    public static HostBuilder Create(IServiceCollection? services = null, ILoggingBuilder? loggingBuilder = null) 
        => new HostBuilder(services, loggingBuilder);

    public Func<IServiceCollection, IServiceProvider>? ConfigureContainer { get; set; }
    public IServiceCollection Services { get; }
    public ILoggingBuilder Logging { get; }
    // public ConfigurationManager Configuration { get; }


    public IHost Build()
    {
#if PLATFORM
        this.Services.AddShinyCoreServices();
#endif
        // this.Services.AddSingleton<IConfiguration>(this.Configuration);

        var serviceProvider = this.ConfigureContainer == null
            ? this.Services.BuildServiceProvider()
            : this.ConfigureContainer.Invoke(this.Services);

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        return new Host(serviceProvider, loggerFactory);
    }
}