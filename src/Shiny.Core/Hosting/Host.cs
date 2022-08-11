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
        private set => currentHost = value ?? throw new ArgumentException(nameof(value));
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
        var logger = this.Logging.CreateLogger<Host>();

        foreach (var task in tasks)
        {
            var tn = task.GetType().FullName;
            try
            {

                task.Start();
                logger.LogDebug($"Startup task '{tn}' ran successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Startup task '{tn}' failed");
            }
        }
        Host.Current = this;
    }

    public void Dispose()
    {
        (this.Services as IDisposable)?.Dispose();
        this.Logging.Dispose();
    }
}