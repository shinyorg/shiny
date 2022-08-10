#if PLATFORM
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Beacons;
using Shiny.Beacons.Infrastructure;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IBeaconRangingManager BeaconRanging(this ShinyContainer container) => container.GetService<IBeaconRangingManager>();
    public static IBeaconMonitoringManager BeaconMonitoring(this ShinyContainer container) => container.GetService<IBeaconMonitoringManager>();

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
        ArgumentNullException.ThrowIfNull(delegateType, "You can't register monitoring regions without a delegate type");

#if ANDROID
        services.TryAddSingleton<BackgroundTask>();
        services.AddBluetoothLE();
#endif
        services.AddSingleton(typeof(IBeaconMonitorDelegate), delegateType);
        services.AddRepository<BeaconRegionStoreConverter, BeaconRegion>();
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