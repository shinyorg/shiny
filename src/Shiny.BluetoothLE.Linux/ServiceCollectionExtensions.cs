using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny;


public static class LinuxBleServiceCollectionExtensions
{
    /// <summary>
    /// Register the IBleManager service for Linux via BlueZ/D-Bus
    /// </summary>
    /// <remarks>
    /// Named apart from Shiny.BluetoothLE's <c>AddBluetoothLE</c>, which registers the Web Bluetooth backend
    /// on the same target framework - this package stays separate so its D-Bus dependency never reaches a
    /// WebAssembly app.
    /// </remarks>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddLinuxBluetoothLE(this IServiceCollection services)
    {
        if (!services.HasImplementation<BleManager>())
        {
            services.AddSingleton<BleManager>();
            services.AddSingleton<IBleManager>(sp => sp.GetRequiredService<BleManager>());
        }

        services.TryAddSingleton<IOperationQueue, SemaphoreOperationQueue>();
        return services;
    }
}
