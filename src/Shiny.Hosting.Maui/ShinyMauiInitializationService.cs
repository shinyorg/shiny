using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Hosting;
using Shiny.Hosting;

namespace Shiny;


public class ShinyMauiInitializationService : IMauiInitializeService
{
    public void Initialize(IServiceProvider services)
    {
#if !PLATFORM
        // MAUI backends outside the platform TFMs (Linux GTK) get NetPlatform, which has no main thread
        // of its own - hand it the backend's dispatcher before any startup task can marshal through it
        var dispatcher = services.GetService<IDispatcherProvider>()?.GetForCurrentThread();
        if (dispatcher != null)
        {
            NetPlatform.MainThreadHandler = action =>
            {
                if (dispatcher.IsDispatchRequired)
                    dispatcher.Dispatch(action);
                else
                    action();
            };
        }
#endif
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var host = new Host(services, loggerFactory);
        host.Run();
    }
}
