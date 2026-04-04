using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny;


public static class MacOSServiceCollectionExtensions
{
    /// <summary>
    /// Register the IBleManager service for macOS via CoreBluetooth
    /// </summary>
    /// <param name="services"></param>
    /// <param name="delegateType"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddBluetoothLE(this IServiceCollection services, Type? delegateType = null, AppleBleConfiguration? config = null)
    {
        services.TryAddSingleton(config ?? new AppleBleConfiguration());

        if (!services.HasImplementation<BleManager>())
            services.AddShinyService<BleManager>();

        if (delegateType != null)
            services.AddShinyService(delegateType);

        services.TryAddSingleton<IOperationQueue, SemaphoreOperationQueue>();
        return services;
    }


    /// <summary>
    /// Register the IBleManager service for macOS via CoreBluetooth with a typed delegate
    /// </summary>
    /// <typeparam name="TCentralDelegate"></typeparam>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddBluetoothLE<TCentralDelegate>(this IServiceCollection services, AppleBleConfiguration? config = null) where TCentralDelegate : class, IBleDelegate
        => services.AddBluetoothLE(typeof(TCentralDelegate), config);
}
