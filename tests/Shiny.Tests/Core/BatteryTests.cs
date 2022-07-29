using Acr.UserDialogs;
using Shiny.Power;
using Shiny.Hosting;

namespace Shiny.Tests.Core;


[Trait("Category", "Battery")]
public class BatteryTests : AbstractShinyTests
{
    public BatteryTests(ITestOutputHelper output) : base(output) {}

    protected override void Configure(IHostBuilder hostBuilder) => hostBuilder.Services.AddBattery();


    [Fact(DisplayName = "Battery - Level (Simulator)")]
    public async Task LevelTest()
    {
        var intLevel = new Random().Next(1, 100);
        var level = (double)intLevel / 100;
        var battery = this.GetService<IBattery>();

        await UserDialogs.Instance.AlertAsync($"Change the battery to {intLevel} in Simulator & Press OK to continue test");
        battery.Level.Should().Be(level);
    }


    [Theory(DisplayName = "Battery - State (Simulator)")]
    [InlineData(BatteryState.Discharging)]
    [InlineData(BatteryState.Charging)]
    [InlineData(BatteryState.NotCharging)]
    public async Task StateTest(BatteryState expectedState)
    {
        await UserDialogs.Instance.AlertAsync($"Change the battery state to {expectedState} in the Simulator & Press OK to continue test");
        var battery = this.GetService<IBattery>();
        if (expectedState == BatteryState.NotCharging && battery.Level == 100)
            battery.Status.Should().Be(BatteryState.Full);
        else
            battery.Status.Should().Be(expectedState);
    }


    [Fact(DisplayName = "Battery - Change Monitoring (Simulator)")]
    public async Task MonitorTest()
    {
        await UserDialogs.Instance.AlertAsync($"Change the battery state or level in the Simulator AFTER pressing OK to pass test");

        var battery = this.GetService<IBattery>();
        await battery
            .WhenChanged()
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(20))
            .ToTask();
    }
}
