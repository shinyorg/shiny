using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Shiny.Bluetooth
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseClassicBluetooth(this IServiceCollection services)
        {
#if NETSTANDARD2_0
            return false;
#else
            //services.TryAddSingleton<IBluetoothManager, ShinyBluetoothManager>();
            return true;
#endif
        }
    }
}
