using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.BluetoothLE.Central
{
    public class BleCentralShinyModule : ShinyModule
    {
        public BleCentralShinyModule(BleCentralConfiguration config) { }


        public override void Register(IServiceCollection services)
        {
            services.AddSingleton<CentralContext>();
            services.AddSingleton<ICentralManager, CentralManager>();
        }
    }
}
