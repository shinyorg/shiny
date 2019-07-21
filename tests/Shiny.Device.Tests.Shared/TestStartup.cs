using System;
using Shiny.BluetoothLE;
using Shiny.Logging;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Device.Tests
{
    public class TestStartup : ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            Log.UseDebug();
            Log.UseConsole();

            services.UseBleCentral();
            services.UseBlePeripherals();
        }
    }
}
