using Sample;
using Shiny.Hosting;
using Shiny.Locations;

namespace Shiny.Tests.Locations;


public class GeofenceManagerTests : AbstractShinyTests
{
    public GeofenceManagerTests(ITestOutputHelper output) : base(output) {}
    protected override void Configure(HostBuilder hostBuilder)
        => hostBuilder.Services.AddGeofencing<SampleGeofenceDelegate>();
    public override async void Dispose()
    {
        await this.GetService<IGeofenceManager>().StopAllMonitoring();
    }


    [Fact(DisplayName = "Geofence - RequestState (run only)")]
    public async Task BasicRunTest()
    {
        await this.GetService<IGeofenceManager>().RequestState(new GeofenceRegion(
            "test",
            new Position(1, 1),
            Distance.FromMeters(300)
        ));
    }


    [Fact(DisplayName = "Geofence - Registration (run only)")]
    public async Task BasicRegistrationTest()
    {
        var manager = this.GetService<IGeofenceManager>();

        await manager.StartMonitoring(new GeofenceRegion(
            nameof(this.BasicRegistrationTest),
            new Position(1, 1),
            Distance.FromMeters(300)
        ));
        var gf = manager.GetMonitorRegions();
        gf.Count.Should().Be(1);

        gf.First().Identifier.Should().Be(nameof(this.BasicRegistrationTest));

        await manager.StopAllMonitoring();
    }
}
