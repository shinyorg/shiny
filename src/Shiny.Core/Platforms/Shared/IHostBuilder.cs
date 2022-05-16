using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Hosting;


public interface IHostBuilder
{
    Func<IServiceCollection, IServiceProvider> ConfigureContainer { get; set; }
    IServiceCollection Services { get; }
    //ConfigurationManager? Configuration { get; }
    ILifecycleBuilder Lifecycle { get; }
    ILoggingBuilder Logging { get; }

    IHost Build();
}
