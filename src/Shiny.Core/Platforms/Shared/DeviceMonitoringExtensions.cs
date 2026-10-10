using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Infrastructure;
using Shiny.Net;
using Shiny.Power;

namespace Shiny;


public static class DeviceMonitoringExtensions
{
#if PLATFORM
    public static void AddConnectivity(this IServiceCollection services)
        => services.TryAddSingleton<IConnectivity, ConnectivityImpl>();

    public static void AddBattery(this IServiceCollection services)
        => services.TryAddSingleton<IBattery, BatteryImpl>();
#else
    /// <summary>
    /// Registers <see cref="IConnectivity"/> - the browser's <c>navigator.onLine</c> and Network
    /// Information API on Blazor WebAssembly, <c>System.Net.NetworkInformation</c> everywhere else.
    /// An <see cref="IConnectivity"/> registered before this call wins.
    /// </summary>
    public static IServiceCollection AddConnectivity(this IServiceCollection services)
    {
        if (services.HasService<IConnectivity>())
            return services;

        if (OperatingSystem.IsBrowser())
        {
            services.TryAddSingleton<ConnectivityManager>();
            services.AddSingleton<IConnectivity>(sp => sp.GetRequiredService<ConnectivityManager>());
        }
        else
        {
            services.AddSingleton<IConnectivity, ConnectivityImpl>();
        }
        return services;
    }


    /// <summary>
    /// Registers <see cref="IBattery"/> - the browser Battery Status API on Blazor WebAssembly, sysfs
    /// (<c>/sys/class/power_supply</c>) on Linux. Plain .NET on Windows and macOS has no battery API,
    /// so nothing is registered there. An <see cref="IBattery"/> registered before this call wins.
    /// </summary>
    public static IServiceCollection AddBattery(this IServiceCollection services)
    {
        if (services.HasService<IBattery>())
            return services;

        if (OperatingSystem.IsBrowser())
        {
            services.TryAddSingleton<BatteryManager>();
            services.AddSingleton<IBattery>(sp => sp.GetRequiredService<BatteryManager>());
        }
        else if (OperatingSystem.IsLinux())
        {
            services.AddSingleton<IBattery, BatteryImpl>();
        }
        return services;
    }
#endif
}
