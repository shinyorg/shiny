using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.BluetoothLE;

namespace Shiny;


public static class ServiceCollectionExtensions
{

    static void AddServices(this IServiceCollection services, BleConfiguration? config)
    {
#if IOS || MACCATALYST || ANDROID
        services.TryAddSingleton(config ?? new BleConfiguration());
        services.TryAddSingleton<Shiny.BluetoothLE.Internals.ManagerContext>();
        services.TryAddSingleton<IBleManager, BleManager>();
#else
        throw new InvalidOperationException("Platform is not supported");
#endif
    }


    /// <summary>
    /// Register the IBleManager service that allows you to connect to other BLE devices - Delegates used here are intended for background usage
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="delegateType"></param>
    /// <returns></returns>
    public static IServiceCollection AddBluetoothLE(this IServiceCollection services, Type? delegateType = null, BleConfiguration? config = null)
    {
        services.AddServices(config);
        if (delegateType != null)
            throw new NotImplementedException();
            //services.AddShinyService(typeof(IBleDelegate), delegateType); // TODO: state restorable

        return services;
    }


    /// <summary>
    /// Register the ICentralManager service that allows you to connect to other BLE devices - Delegates used here are intended for background usage
    /// </summary>
    /// <typeparam name="TCentralDelegate"></typeparam>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddBluetoothLE<TCentralDelegate>(this IServiceCollection services, BleConfiguration? config = null) where TCentralDelegate : class, IBleDelegate
        => services.AddBluetoothLE(typeof(TCentralDelegate), config);
}