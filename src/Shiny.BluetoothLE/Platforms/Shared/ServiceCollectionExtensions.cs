using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.BluetoothLE;

namespace Shiny;


public static class ServiceCollectionExtensions
{

    public static IBleManager BluetoothLE(this ShinyContainer container) => container.GetService<IBleManager>();


    static bool AddServices(this IServiceCollection services, BleConfiguration? config)
    {
#if IOS || MACCATALYST || ANDROID
        services.TryAddSingleton(config ?? new BleConfiguration());

        // TODO: lifecycle method for direct impl only
        services.TryAddSingleton<Shiny.BluetoothLE.Internals.ManagerContext>();
        services.AddShinyServiceWithLifecycle<IBleManager, BleManager>();
        return true;
#else
        return false;
#endif
    }


    /// <summary>
    /// Register the IBleManager service that allows you to connect to other BLE devices - Delegates used here are intended for background usage
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="delegateType"></param>
    /// <returns></returns>
    public static bool AddBluetoothLE(this IServiceCollection services, Type? delegateType = null, BleConfiguration? config = null)
    {
        if (!services.AddServices(config))
            return false;

        if (delegateType != null)
            services.AddShinyService(typeof(IBleDelegate), delegateType);

        return true;
    }


    /// <summary>
    /// Register the ICentralManager service that allows you to connect to other BLE devices - Delegates used here are intended for background usage
    /// </summary>
    /// <typeparam name="TCentralDelegate"></typeparam>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static bool AddBluetoothLE<TCentralDelegate>(this IServiceCollection services, BleConfiguration? config = null) where TCentralDelegate : class, IBleDelegate
        => services.AddBluetoothLE(typeof(TCentralDelegate), config);
}