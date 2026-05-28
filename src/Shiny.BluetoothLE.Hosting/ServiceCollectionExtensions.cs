#if PLATFORM
using Microsoft.Extensions.DependencyInjection;
using Shiny.BluetoothLE.Hosting;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the IBleHostingManager service that allows you to act as a BLE peripheral.
    /// </summary>
    public static IServiceCollection AddBluetoothLeHosting(this IServiceCollection services)
    {

        if (!services.HasService<IBleHostingManager>())
            services.AddSingletonAsImplementedInterfaces<BleHostingManager>(); 
        return services;
    }
}
#endif