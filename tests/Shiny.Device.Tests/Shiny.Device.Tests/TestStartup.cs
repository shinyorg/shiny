using Shiny.Logging;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Device.Tests.Localization;
using Shiny.Device.Tests.Localization.OtherResources;
using Shiny.Localization;
using Shiny.Localization.Resx;


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

            services.UseLocalization<ResxTextProvider<OtherTextResources>>(optionsBuilder =>
                optionsBuilder.WithAutoInitialization(false)
                    .AddTextProvider<ResxTextProvider<OtherTextResources>>());
        }
    }
}
