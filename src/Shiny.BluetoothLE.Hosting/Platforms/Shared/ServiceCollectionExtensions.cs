using System.Linq;
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
        if (!services.Any(x => x.ServiceType == typeof(IBleHostingManager)))
            services.AddShinyService<BleHostingManager>();

        return services;
    }
}
