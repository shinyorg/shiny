using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Net;
using Shiny.Power;

namespace Shiny;


public static class LinuxHttpServiceCollectionExtensions
{
    /// <summary>
    /// Registers IBattery for Linux backed by sysfs (/sys/class/power_supply).
    /// </summary>
    public static IServiceCollection AddBattery(this IServiceCollection services)
    {
        services.TryAddSingleton<IBattery, BatteryImpl>();
        return services;
    }


    /// <summary>
    /// Registers IConnectivity for Linux backed by System.Net.NetworkInformation.
    /// </summary>
    public static IServiceCollection AddConnectivity(this IServiceCollection services)
    {
        services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
        return services;
    }
}
