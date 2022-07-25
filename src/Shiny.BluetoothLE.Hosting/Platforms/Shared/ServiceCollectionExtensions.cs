using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.BluetoothLE.Hosting;
using Shiny.BluetoothLE.Hosting.Managed;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IBleHostingManager BleHosting(this ShinyContainer container) => container.GetService<IBleHostingManager>();


    /// <summary>
    /// Registers the IPeripheralManager service that allows you to be a host BLE device
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static bool AddBluetoothLeHosting(this IServiceCollection services)
    {
#if IOS || ANDROID || MACCATALYST
        if (!services.Any(x => x.ServiceType == typeof(IBleHostingManager)))
            services.AddShinyService<BleHostingManager>();

        return true;
#else
        return false;
#endif
    }


    // TODO: we need to know PSM
    //public static void AddBleHostedL2Channel<TDelegate>(this IServiceCollection services, bool secure) where TDelegate : IL2CapEndpointDelegate
    //{
    //    // TODO: I could cheat and put some sort of context object in the services and pull it out to add services
    //}


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <param name="services"></param>
    public static void AddBleHostedCharacteristic<TService>(this IServiceCollection services) where TService : BleGattCharacteristic
    {
        services.AddBluetoothLeHosting();
        services.AddSingleton<BleGattCharacteristic, TService>();
    }
}
