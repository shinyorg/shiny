using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the IBleManager service for Linux via BlueZ/D-Bus
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddBluetoothLE(this IServiceCollection services)
    {
        if (!services.HasImplementation<BleManager>())
            services.AddShinyService<BleManager>();

        services.TryAddSingleton<IOperationQueue, SemaphoreOperationQueue>();
        return services;
    }
}
