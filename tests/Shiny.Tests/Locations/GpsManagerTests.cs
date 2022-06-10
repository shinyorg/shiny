using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using FluentAssertions;
using Shiny.Hosting;
using Shiny.Locations;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Tests.Locations;


public class GpsManagerTests : AbstractShinyTests
{
    public GpsManagerTests(ITestOutputHelper output) : base(output) { }
    protected override void Configure(IHostBuilder hostBuilder) => hostBuilder.Services.AddGps();


    [Fact(DisplayName = "GPS - Last Location")]
    public async Task GetLastLocationTest()
    {
        var reading = await this.GetService<IGpsManager>()
            .GetLastReading()
            .Timeout(TimeSpan.FromSeconds(15))
            .ToTask();

        reading.Should().NotBeNull();
    }
}
