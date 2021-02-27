#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.BluetoothLE
{
    public class BleShinyModule : ShinyModule
    {
        readonly BleConfiguration config;
        public BleShinyModule(BleConfiguration config) => this.config = config ?? new BleConfiguration();


        public override void Register(IServiceCollection services)
        {
            services.AddSingleton(this.config);
            services.TryAddSingleton<Internals.ManagerContext>();
            services.TryAddSingleton<IBleManager, BleManager>();
        }
    }
}
#endif