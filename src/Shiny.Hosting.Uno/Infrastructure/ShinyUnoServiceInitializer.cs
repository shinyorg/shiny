using System;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Uno.Extensions.Hosting;

namespace Shiny.Infrastructure;


/// <summary>
/// Uno runs every <see cref="IServiceInitialize"/> straight after it builds the container - the Uno equivalent of
/// MAUI's <c>IMauiInitializeService</c> - so Shiny's startup tasks run whether or not the app ever starts the host.
/// </summary>
sealed class ShinyUnoServiceInitializer(IServiceProvider services, ILoggerFactory loggerFactory) : IServiceInitialize
{
    public void Initialize()
    {
        if (ShinyUnoHost.TryClaim(services))
            new Host(services, loggerFactory).Run();
    }
}
