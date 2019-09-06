using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.BluetoothLE.Central.Internals;


namespace Shiny.BluetoothLE.Central
{
    public class BleCentralShinyModule : ShinyModule
    {
        readonly BleCentralConfiguration config;
        public BleCentralShinyModule(BleCentralConfiguration config = null) => this.config = config;


        public override void Register(IServiceCollection services)
        {
            if (this.config == null)
                services.AddSingleton(this.config);

            services.AddSingleton<CentralContext>();
            services.AddSingleton<ICentralManager, CentralManager>();
        }
    }
}
