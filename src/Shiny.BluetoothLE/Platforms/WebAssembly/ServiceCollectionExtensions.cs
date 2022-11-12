using Microsoft.Extensions.DependencyInjection;
using Shiny.BluetoothLE;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBluetoothLE(this IServiceCollection services)
    {
        services.AddShinyService<BleManager>();
        return services;
    }
}
