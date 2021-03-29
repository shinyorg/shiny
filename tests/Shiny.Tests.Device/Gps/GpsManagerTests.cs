using System;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Shiny.Locations;
using Xunit;


namespace Shiny.Tests.Gps
{
    public class GpsManagerTests
    {
        readonly IGpsManager gps;


        public GpsManagerTests()
        {
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = x => x.UseGps()
            });
            this.gps = ShinyHost.Resolve<IGpsManager>();
        }


        [Fact]
        public async Task GetCurrentLocationTest()
        {
            var reading = await this.gps.GetLastReading().ToTask();
        }
    }
}
