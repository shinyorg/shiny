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
        this.Services.AddShinyCoreServices();

        var serviceProvider = this.ConfigureContainer(this.Services);
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var host = new Host(serviceProvider, loggerFactory);

        return host;
    }
}