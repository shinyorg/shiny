using System;
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

            services.UseBleClient();
            //services.UseBleHosting();
        }
    }
}
