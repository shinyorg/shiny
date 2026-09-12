using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Infrastructure;
using Shiny.Net;
using Shiny.Power;

namespace Shiny;


public static class BlazorServiceCollectionExtensions
{
    public static IServiceCollection AddConnectivity(this IServiceCollection services)
    {
        services.TryAddSingleton<ConnectivityManager>();
        services.TryAddSingleton<IConnectivity>(sp => sp.GetRequiredService<ConnectivityManager>());
        return services;
    }

    public static IServiceCollection AddBattery(this IServiceCollection services)
    {
        services.TryAddSingleton<BatteryManager>();
        services.TryAddSingleton<IBattery>(sp => sp.GetRequiredService<BatteryManager>());
        return services;
    }


    /// <summary>
    /// Starts whichever of the Blazor connectivity/battery monitors have been registered, so that
    /// they report real values from the first read rather than after their JS module has loaded in
    /// the background.
    /// </summary>
    /// <remarks>
    /// Optional - both monitors start themselves the first time they are read or subscribed to.
    /// Call this right after <c>builder.Build()</c> when you want the first read to be accurate.
    /// </remarks>
    public static async Task UseShinyCore(this IServiceProvider services)
    {
        var connectivity = services.GetService<ConnectivityManager>();
        if (connectivity != null)
            await connectivity.StartAsync().ConfigureAwait(false);

        var battery = services.GetService<BatteryManager>();
        if (battery != null)
            await battery.StartAsync().ConfigureAwait(false);
    }
}
