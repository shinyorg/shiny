using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Shiny.Tests
{
    public class TestStartup : ShinyStartup
    {
        public static IPlatform CurrentPlatform { get; private set; }


        public override void ConfigureLogging(ILoggingBuilder builder, IPlatform platform)
        {

            //https://github.com/yorchideas/Xunit.Extensions.Logging
        }


        public override void ConfigureServices(IServiceCollection services, IPlatform platform)
        {
            //services.UseBleClient();
            //services.UseSqliteStore();
            //services.UseBleHosting();
            CurrentPlatform = platform;
        }
    }
}
