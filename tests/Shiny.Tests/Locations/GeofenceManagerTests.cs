using Sample;
using Shiny.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Shiny.Tests.Locations;


public class GeofenceManagerTests : AbstractShinyTests
{
    public GeofenceManagerTests(ITestOutputHelper output) : base(output) {}


    protected override void Configure(IHostBuilder hostBuilder)
        => hostBuilder.Services.AddGeofencing<SampleGeofenceDelegate>();


    [Fact]
    public async Task GetCurrent()
    {

    }
}
