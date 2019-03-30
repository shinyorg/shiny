using System;
using Acr.BluetoothLE.Central;
using Acr.Settings;
using Microsoft.Extensions.DependencyInjection;


namespace Acr
{
    public static class Extensions
    {
        public static IServiceCollection AddBleCentral(this IServiceCollection services)
            => services.AddSingleton<ICentralManager, CentralManager>();

        public static IServiceCollection AddSettings(this IServiceCollection services)
            => services.AddSingleton<ISettings, SettingsImpl>();
    }
}
