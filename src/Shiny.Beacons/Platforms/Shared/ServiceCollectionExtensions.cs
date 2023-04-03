#if PLATFORM
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Beacons;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the beacon service with this if you only plan to use ranging
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddBeaconRanging(this IServiceCollection services)
    {
#if ANDROID
        services.AddBluetoothLE();
#endif
        services.TryAddSingleton<IBeaconRangingManager, BeaconRangingManager>();
        return services;
    }


    /// <summary>
    /// Use this method if you plan to use background monitoring (works for ranging as well)
    /// </summary>
    /// <param name="services"></param>
    /// <param name="delegateType"></param>
    /// <returns></returns>
    public static IServiceCollection AddBeaconMonitoring(this IServiceCollection services, Type delegateType)
    {
        if (delegateType == null)
            throw new ArgumentNullException(nameof(delegateType), "You can't register monitoring regions without a delegate type");

#if ANDROID
        services.TryAddSingleton<BackgroundTask>();
        services.AddBluetoothLE();
#endif
        services.AddDefaultRepository();
        services.AddSingleton(typeof(IBeaconMonitorDelegate), delegateType);
        services.TryAddSingleton<IBeaconMonitoringManager, BeaconMonitoringManager>();

        return services;
    }


    /// <summary>
    /// Use this method if you plan to use background monitoring (works for ranging as well)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddBeaconMonitoring<T>(this IServiceCollection services) where T : class, IBeaconMonitorDelegate
        => services.AddBeaconMonitoring(typeof(T));
}
#endif