using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Hosting;

namespace Shiny.Infrastructure;


/// <summary>
/// Starts the browser monitors Shiny has registered - connectivity, battery, screen info, screen posture - before the
/// first page: Uno.Extensions navigation awaits every <see cref="IStartupService"/> before it navigates (behind the
/// Toolkit splash, when there is one). The Uno equivalent of <c>await services.UseShiny()</c> in a Blazor app; without
/// it the monitors report Unknown until their JS module has loaded, and the first page reads that.
/// </summary>
sealed class ShinyBrowserStartup(IServiceProvider services, ILogger<ShinyBrowserStartup> logger) : IStartupService
{
    Task? startup;


    public Task StartupComplete() => this.startup ??= this.Start();


    async Task Start()
    {
        // a monitor that fails to load keeps reporting Unknown - it must not hold up the first navigation
        try
        {
            var connectivity = services.GetService<ConnectivityManager>();
            if (connectivity != null)
                await connectivity.StartAsync().ConfigureAwait(false);

            var battery = services.GetService<BatteryManager>();
            if (battery != null)
                await battery.StartAsync().ConfigureAwait(false);

            var screen = services.GetService<BrowserScreenInfo>();
            if (screen != null)
                await screen.StartAsync().ConfigureAwait(false);

            var posture = services.GetService<BrowserScreenPosture>();
            if (posture != null)
                await posture.StartAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to start Shiny's browser monitors");
        }
    }
}
