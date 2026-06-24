using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny;


public static class BleServiceCollectionExtensions
{
#if APPLE || ANDROID || WINDOWS
    /// <summary>
    /// Register the IBleManager service that allows you to connect to other BLE devices
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
#if APPLE
    public static IServiceCollection AddBluetoothLE(this IServiceCollection services, AppleBleConfiguration? config = null)
    {
        services.TryAddSingleton(config ?? new AppleBleConfiguration());

#elif ANDROID || WINDOWS
    public static IServiceCollection AddBluetoothLE(this IServiceCollection services)
    {
#endif
        if (!services.HasImplementation<BleManager>())
            services.AddSingletonAsImplementedInterfaces<BleManager>();

        services.TryAddSingleton<IOperationQueue, SemaphoreOperationQueue>();
        
        return services;
    }

    
#if APPLE
    /// <summary>
    /// Register the ICentralManager service that allows you to connect to other BLE devices - Delegates used here are intended for background usage
    /// </summary>
    /// <typeparam name="TCentralDelegate"></typeparam>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddBluetoothLE<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TCentralDelegate>(
        this IServiceCollection services,
        AppleBleConfiguration? config = null
    ) where TCentralDelegate : class, IBleDelegate
    {
        services.AddBluetoothLE(config);
        return services.AddSingletonAsImplementedInterfaces<TCentralDelegate>();
    }
#else
    /// <summary>
    /// Register the ICentralManager service that allows you to connect to other BLE devices - Delegates used here are intended for background usage
    /// </summary>
    /// <typeparam name="TCentralDelegate"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddBluetoothLE<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TCentralDelegate>(this IServiceCollection services) where TCentralDelegate : class, IBleDelegate
    {
        services.AddBluetoothLE();
        return services.AddSingletonAsImplementedInterfaces<TCentralDelegate>();
    }
#endif
#else
    public static IServiceCollection AddBluetoothLE<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TCentralDelegate>(this IServiceCollection services) where TCentralDelegate : class, IBleDelegate
        => services;
    
    public static IServiceCollection AddBluetoothLE(this IServiceCollection services)
        => services;
#endif
}
