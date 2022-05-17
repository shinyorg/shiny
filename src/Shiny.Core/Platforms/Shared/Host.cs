using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Hosting;


public class Host : IHost
{
    const string InitFailErrorMessage = "ServiceProvider is not initialized - This means you have not setup Shiny correctly!  Please follow instructions at https://shinylib.net";


    static IHost? currentHost;
    public static IHost Current
    {
        get
        {
            if (currentHost == null)
                throw new InvalidOperationException(InitFailErrorMessage);

            return currentHost;
        }
        private set
        {
            ArgumentNullException.ThrowIfNull(value);
            currentHost = value;
        }
    }

    public static bool IsInitialized => currentHost != null;


    public Host(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        this.Services = serviceProvider;
        this.Logging = loggerFactory;
    }


    public IServiceProvider Services { get; init; }
    public ILoggerFactory Logging { get; init; }


    public virtual void Run()
    {
        var tasks = this.Services.GetServices<IShinyStartupTask>();

        foreach (var task in tasks)
        {
            // TODO: log this
            task.Start();
        }
        Host.Current = this;
    }
}