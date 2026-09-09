using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Beacons;

namespace Shiny;


/// <summary>
/// Registration for iBeacon and Eddystone services.
/// </summary>
public static class BeaconServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IBeaconRangingManager"/> for foreground iBeacon ranging.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Distance, filtering and threshold settings.</param>
    /// <remarks>
    /// On Apple platforms this uses CoreLocation and needs a location permission plus an
    /// <c>NSLocationWhenInUseUsageDescription</c> entry in Info.plist. Everywhere else it scans for
    /// BLE advertisements directly and needs Bluetooth permissions instead.
    /// </remarks>
    public static IServiceCollection AddBeaconRanging(this IServiceCollection services, BeaconRangingOptions? options = null)
    {
        services.AddBeaconOptions(options);

#if APPLE
        if (!services.HasService<IBeaconRangingManager>())
            services.AddSingletonAsImplementedInterfaces<BeaconRangingManager>();
#else
        services.EnsureBleManager();

        if (!services.HasService<IBeaconRangingManager>())
            services.AddSingletonAsImplementedInterfaces<BleScanBeaconRangingManager>();
#endif
        return services;
    }


    /// <summary>
    /// Registers <see cref="IBeaconMonitoringManager"/> for background beacon region monitoring,
    /// along with the delegate that receives enter and exit transitions.
    /// </summary>
    /// <typeparam name="TDelegate">The delegate invoked on region transitions.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Distance, filtering and threshold settings.</param>
    /// <remarks>
    /// <para>
    /// Monitoring needs stronger permissions than ranging - always-on location on Apple platforms,
    /// and a foreground service on Android. Ranging keeps working once this is registered.
    /// </para>
    /// <para>
    /// macOS has no beacon region API in CoreLocation. Registration still succeeds so the same
    /// startup code runs everywhere, but the manager reports
    /// <see cref="AccessState.NotSupported"/> and refuses to monitor. Ranging and Eddystone
    /// scanning both work on macOS.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddBeaconMonitoring<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.Interfaces)] TDelegate>(
        this IServiceCollection services,
        BeaconRangingOptions? options = null
    ) where TDelegate : class, IBeaconMonitorDelegate
    {
        services.AddBeaconOptions(options);
        services.AddDefaultRepository();
        services.AddSingletonAsImplementedInterfaces<TDelegate>();

        if (services.HasService<IBeaconMonitoringManager>())
            return services;

#if ANDROID
        services.EnsureBleManager();
        services.AddSingletonAsImplementedInterfaces<AndroidBeaconMonitoringManager>();

#elif MACOS
        // CoreLocation on the Mac has no beacon region API at all. Registered anyway so shared
        // startup code runs, reporting NotSupported through AccessState like any other capability.
        services.AddSingletonAsImplementedInterfaces<MacOSBeaconMonitoringManager>();

#elif APPLE
        if (OperatingSystem.IsIOSVersionAtLeast(18) || OperatingSystem.IsMacCatalystVersionAtLeast(18))
            services.AddSingletonAsImplementedInterfaces<BeaconMonitoringManager>();
        else
            services.AddSingletonAsImplementedInterfaces<CLLocationBeaconMonitoringManager>();

#else
        services.EnsureBleManager();
        services.AddSingletonAsImplementedInterfaces<BleScanBeaconMonitoringManager>();
#endif
        return services;
    }


    /// <summary>
    /// Registers <see cref="IEddystoneScanner"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Distance, filtering and threshold settings.</param>
    /// <remarks>
    /// Works on every platform including Apple's, because Eddystone travels as BLE service data
    /// rather than the manufacturer data CoreBluetooth filters out.
    /// </remarks>
    public static IServiceCollection AddEddystoneScanning(this IServiceCollection services, BeaconRangingOptions? options = null)
    {
        services.AddBeaconOptions(options);
        services.EnsureBleManager();

        if (!services.HasService<IEddystoneScanner>())
            services.AddSingletonAsImplementedInterfaces<BleEddystoneScanner>();

        return services;
    }


    /// <summary>
    /// Registers <see cref="IBeaconBroadcaster"/> so the device can act as a beacon itself.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <remarks>
    /// Apple platforms can broadcast iBeacon but not Eddystone, and stop broadcasting a decodable
    /// beacon once the app is backgrounded. Linux cannot broadcast at all yet.
    /// </remarks>
    public static IServiceCollection AddBeaconBroadcasting(this IServiceCollection services)
    {
#if PLATFORM
        services.AddBluetoothLeHosting();
#endif
        if (!services.HasService<IBeaconBroadcaster>())
            services.AddSingletonAsImplementedInterfaces<BeaconBroadcaster>();

        return services;
    }


    static void AddBeaconOptions(this IServiceCollection services, BeaconRangingOptions? options)
        => services.TryAddSingleton(options ?? new BeaconRangingOptions());


    static void EnsureBleManager(this IServiceCollection services)
    {
#if APPLE || ANDROID || WINDOWS
        services.AddBluetoothLE();
#else
        // The base target has no BLE implementation of its own - a Linux or plain .NET host brings
        // one (Shiny.BluetoothLE.Linux) and registers it. Say so now rather than failing later with
        // an unresolved IBleManager from somewhere deep in a scan.
        if (!services.HasService<Shiny.BluetoothLE.IBleManager>())
            throw new InvalidOperationException("No IBleManager is registered. Beacons need a BluetoothLE implementation - call AddBluetoothLE() from Shiny.BluetoothLE.Linux (or your host's equivalent) before registering beacon services.");
#endif
    }
}
