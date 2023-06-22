using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the IBleManager service that allows you to connect to other BLE devices - Delegates used here are intended for background usage
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="delegateType"></param>
    /// <returns></returns>
#if APPLE
    public static IServiceCollection AddBluetoothLE(this IServiceCollection services, Type? delegateType = null, AppleBleConfiguration? config = null)
    {
        services.TryAddSingleton(config ?? new AppleBleConfiguration());

#elif ANDROID || WINDOWS
    public static IServiceCollection AddBluetoothLE(this IServiceCollection services, Type? delegateType = null)
    {       
#endif
        if (!services.HasImplementation<BleManager>())
            services.AddShinyService<BleManager>();

        if (delegateType != null)
            services.AddShinyService(delegateType);

        services.TryAddSingleton<IOperationQueue, SemaphoreOperationQueue>();
        return services;
    }


    /// <summary>
    /// Register the ICentralManager service that allows you to connect to other BLE devices - Delegates used here are intended for background usage
    /// </summary>
    /// <typeparam name="TCentralDelegate"></typeparam>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
#if APPLE
    public static IServiceCollection AddBluetoothLE<TCentralDelegate>(this IServiceCollection services, AppleBleConfiguration? config = null) where TCentralDelegate : class, IBleDelegate
        => services.AddBluetoothLE(typeof(TCentralDelegate), config);

#else
    public static IServiceCollection AddBluetoothLE<TCentralDelegate>(this IServiceCollection services) where TCentralDelegate : class, IBleDelegate
        => services.AddBluetoothLE(typeof(TCentralDelegate));
#endif
}