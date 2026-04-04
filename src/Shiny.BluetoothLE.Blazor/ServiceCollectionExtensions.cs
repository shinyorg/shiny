using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.BluetoothLE;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the IBleManager service for Blazor WebAssembly via Web Bluetooth API
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddBluetoothLE(this IServiceCollection services)
    {
        services.TryAddSingleton<IBleManager, BleManager>();
        return services;
    }
}
