using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Hosting;


public class Host : IHost
{
    const string InitFailErrorMessage = "ServiceProvider is not initialized - This means you have not setup Shiny correctly!  Please follow instructions at https://shinylib.net";


    static readonly object runLock = new();
    static IServiceProvider? runningServices;
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
    public static IServiceProvider ServiceProvider => Current.Services;
    public static ILoggerFactory LoggingFactory => Current.Logging;
    public static T? GetService<T>() => ServiceProvider.GetService<T>();

#if IOS || MACCATALYST || TVOS
    public static IosPlatform Platform => ServiceProvider.GetRequiredService<IosPlatform>();
    public static IosLifecycleExecutor Lifecycle => ServiceProvider.GetRequiredService<IosLifecycleExecutor>();
#elif MACOS
    public static MacPlatform Platform => ServiceProvider.GetRequiredService<MacPlatform>();
    public static MacLifecycleExecutor Lifecycle => ServiceProvider.GetRequiredService<MacLifecycleExecutor>();
#elif ANDROID
    public static AndroidPlatform Platform => ServiceProvider.GetRequiredService<AndroidPlatform>();
    public static AndroidLifecycleExecutor Lifecycle => ServiceProvider.GetRequiredService<AndroidLifecycleExecutor>();
#endif


    public Host(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        this.Services = serviceProvider;
        this.Logging = loggerFactory;
    }


    public IServiceProvider Services { get; init; }
    public ILoggerFactory Logging { get; init; }


    public virtual void Run()
    {
        var logger = this.Services.GetRequiredService<ILogger<Host>>();

        lock (runLock)
        {
            // a second host over the same container (UseShiny() called twice, or UseShiny() next to a
            // hand-written initializer) would start every startup task again and double up their hooks
            if (ReferenceEquals(runningServices, this.Services))
            {
                logger.LogWarning("Shiny host is already running for this service provider - ignoring the additional Run() call");
                return;
            }
            runningServices = this.Services;

            try
            {
                var tasks = this.Services.GetServices<IShinyStartupTask>();
                foreach (var task in tasks)
                {
                    var tn = task.GetType().FullName;
                    logger.LogDebug($"Startup task '{tn}' ran successfully");
                    task.Start();
                }
                Host.Current = this;
            }
            catch
            {
                runningServices = null;
                throw;
            }
        }
    }


    public void Dispose()
    {
        (this.Services as IDisposable)?.Dispose();
        this.Logging.Dispose();
        currentHost = null;
        lock (runLock)
        {
            if (ReferenceEquals(runningServices, this.Services))
                runningServices = null;
        }
    }
}