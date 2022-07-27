using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Net;
using Shiny.Power;

namespace Shiny;


public static class ServiceRegistrationExtensions
{
    public static bool AddBattery(this IServiceCollection services)
    {
#if IOS || MACCATALYST || ANDROID
        services.TryAddSingleton<IBatteryManager, BatteryManagerImpl>();
        return true;
#else
        return false;
#endif
    }


    public static bool AddConnectivity(this IServiceCollection services)
    {
#if IOS || MACCATALYST || ANDROID
        services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
        return true;
#else
        return false;
#endif
    }
}

