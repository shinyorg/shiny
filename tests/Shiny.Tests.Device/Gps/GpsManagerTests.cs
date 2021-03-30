using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Shiny.Locations;
using Xunit;
using Xunit.Abstractions;


namespace Shiny.Tests.Gps
{
    public class GpsManagerTests
    {
        readonly IGpsManager gps;


        public GpsManagerTests(ITestOutputHelper output)
        {
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = x => x.UseGps(),
                BuildLogging = x => x.AddXUnit(output)
            });
            this.gps = ShinyHost.Resolve<IGpsManager>();
        }


        [Fact(DisplayName = "GPS - Last Location")]
        public async Task GetLastLocationTest()
        {
            var reading = await this.gps
                .GetLastReading()
                .Timeout(TimeSpan.FromSeconds(15))
                .ToTask();

            reading.Should().NotBeNull();
        }
    }
}
