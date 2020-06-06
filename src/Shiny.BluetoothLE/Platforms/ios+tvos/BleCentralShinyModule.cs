using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.BluetoothLE.Central
{
    public class BleCentralShinyModule : ShinyModule
    {
        readonly BleCentralConfiguration config;
        public BleCentralShinyModule(BleCentralConfiguration config) => this.config = config;


        public override void Register(IServiceCollection services)
        {
            services.AddSingleton(this.config);
            services.AddSingleton<CentralContext>();
            services.AddSingleton<ICentralManager, CentralManager>();
        }
    }
}
