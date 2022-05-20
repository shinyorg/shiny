using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Beacons;
using Shiny.Beacons.Infrastructure;
using Shiny.Stores;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register the beacon service with this if you only plan to use ranging
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static bool AddBeaconRanging(this IServiceCollection services)
    {
#if !IOS && !MACCATALYST && !ANDROID
        return false;
#else
#if ANDROID
        services.AddBluetoothLE();
#endif
        services.TryAddSingleton<IBeaconRangingManager, BeaconRangingManager>();
        return true;
#endif
    }


    /// <summary>
    /// Use this method if you plan to use background monitoring (works for ranging as well)
    /// </summary>
    /// <param name="services"></param>
    /// <param name="delegateType"></param>
    /// <returns></returns>
    public static bool AddBeaconMonitoring(this IServiceCollection services, Type delegateType)
    {
#if !IOS && !MACCATALYST && !ANDROID
        return false;
#else
        ArgumentNullException.ThrowIfNull(delegateType, "You can't register monitoring regions without a delegate type");

#if ANDROID
        services.TryAddSingleton<BackgroundTask>();
        services.AddBluetoothLE();
#endif
        services.AddSingleton(typeof(IBeaconMonitorDelegate), delegateType);
        services.AddRepository<BeaconRegionStoreConverter, BeaconRegion>();
        services.TryAddSingleton<IBeaconMonitoringManager, BeaconMonitoringManager>();
        return true;
#endif
    }


    /// <summary>
    /// Use this method if you plan to use background monitoring (works for ranging as well)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static bool AddBeaconMonitoring<T>(this IServiceCollection services) where T : class, IBeaconMonitorDelegate
        => services.AddBeaconMonitoring(typeof(T));
}
