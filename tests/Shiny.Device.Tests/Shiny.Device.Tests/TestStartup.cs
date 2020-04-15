using Shiny.Logging;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Device.Tests.Localization;
using Shiny.Localization;


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

            services.UseLocalization<ResxTextProvider<DeviceTextResources>>(optionsBuilder => optionsBuilder.EnableAutoInitialization(false));
        }
    }
}
