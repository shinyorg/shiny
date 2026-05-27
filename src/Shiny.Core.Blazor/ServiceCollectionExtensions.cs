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
        services.TryAddSingleton<IConnectivity, ConnectivityManager>();
        return services;
    }

    public static IServiceCollection AddBattery(this IServiceCollection services)
    {
        services.TryAddSingleton<IBattery, BatteryManager>();
        return services;
    }
}
