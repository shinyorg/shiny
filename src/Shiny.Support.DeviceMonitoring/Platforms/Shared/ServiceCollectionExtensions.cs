using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Net;
using Shiny.Power;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static void AddConnectivity(this IServiceCollection services)
        => services.TryAddSingleton<IConnectivity, ConnectivityImpl>();

    public static void AddBattery(this IServiceCollection services)
        => services.TryAddSingleton<IBattery, BatteryImpl>();
}