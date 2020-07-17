#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.BluetoothLE
{
    public class BleShinyModule : ShinyModule
    {
        readonly BleConfiguration config;
        public BleShinyModule(BleConfiguration config) => this.config = config ?? new BleConfiguration();


        public override void Register(IServiceCollection services)
        {
            services.AddSingleton(this.config);
#if __ANDROID__
            services.AddSingleton<Shiny.BluetoothLE.Internals.CentralContext>();
#else
            services.AddSingleton<CentralContext>();
#endif
            services.AddSingleton<IBleManager, BleManager>();
        }
    }
}
#endif