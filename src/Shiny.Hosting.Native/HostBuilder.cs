using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Hosting;
#if MONOANDROID || XAMARINIOS
using Shiny.Infrastructure;
using Shiny.Net;
using Shiny.Power;
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
#if MONOANDROID || XAMARINIOS
        this.Services.AddShinyCoreServices();
        this.Services.TryAddSingleton<IBattery, BatteryImpl>();
        this.Services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
#endif
        this.Services.AddSingleton<IConfiguration>(this.Configuration);

        var serviceProvider = this.ConfigureContainer == null
            ? this.Services.BuildServiceProvider()
            : this.ConfigureContainer.Invoke(this.Services);

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        return new Host(serviceProvider, loggerFactory);
    }
}