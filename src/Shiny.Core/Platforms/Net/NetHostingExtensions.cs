using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Shiny.Infrastructure;

namespace Shiny;


public static class NetHostingExtensions
{
    /// <summary>
    /// Starts the Shiny host over an already-built container - running every registered
    /// <see cref="IShinyStartupTask"/>, such as the in-process job runner - then, on Blazor
    /// WebAssembly, starts whichever browser connectivity/battery monitors have been registered so
    /// they report real values from the first read rather than after their JS module has loaded.
    /// </summary>
    /// <remarks>
    /// On Blazor WebAssembly, call this right after <c>builder.Build()</c> and before
    /// <c>RunAsync()</c>. Without it, startup tasks never run; the monitors still start themselves the
    /// first time they are read or subscribed to. Calling it again for the same container does not
    /// start anything twice.
    /// </remarks>
    public static async Task UseShiny(this IServiceProvider services)
    {
        new Host(services, services.GetRequiredService<ILoggerFactory>()).Run();

        var connectivity = services.GetService<ConnectivityManager>();
        if (connectivity != null)
            await connectivity.StartAsync().ConfigureAwait(false);

        var battery = services.GetService<BatteryManager>();
        if (battery != null)
            await battery.StartAsync().ConfigureAwait(false);
    }


    /// <summary>
    /// Renamed to <see cref="UseShiny"/>, to match the MAUI hosting call.
    /// </summary>
    [Obsolete("Use UseShiny() - it does the same thing")]
    public static Task UseShinyCore(this IServiceProvider services) => services.UseShiny();
}
