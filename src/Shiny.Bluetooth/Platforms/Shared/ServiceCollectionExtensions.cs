using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Bluetooth
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseBluetoothClassic(this IServiceCollection services)
        {
#if __IOS__
            if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                services.AddSingleton<IBluetoothManager, BluetoothManagerImpl>();
                return true;
            }
            return false;
#elif __ANDROID__ || WINDOWS_UWP
            services.AddSingleton<IBluetoothManager, BluetoothManagerImpl>();
            return true;
#else
            return false;
#endif
        }
    }
}
