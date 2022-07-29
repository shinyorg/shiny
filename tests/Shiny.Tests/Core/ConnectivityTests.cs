using Acr.UserDialogs;
using Shiny.Hosting;
using Shiny.Net;

namespace Shiny.Tests.Core;


public class ConnectivityTests : AbstractShinyTests
{
    public ConnectivityTests(ITestOutputHelper output) : base(output) {}
    protected override void Configure(IHostBuilder hostBuilder) => hostBuilder.Services.AddConnectivity();


    [Theory(DisplayName = "Connectivity - Access")]
    [InlineData(NetworkAccess.Internet, "Ensure you have internet connectivity")]
    [InlineData(NetworkAccess.None, "Disconnect from WIFI, disable cellular data, ")]
    public async Task StateTests(NetworkAccess expectedAccess, string message)
    {
        var conn = this.GetService<IConnectivity>();
        await UserDialogs.Instance.AlertAsync(message + " & then press OK");
        conn.Access.Should().Be(expectedAccess);
    }


    [Theory(DisplayName = "Connectivity - Connection Types")]
    [InlineData(ConnectionTypes.Wifi, "Connect to WIFI")]
    [InlineData(ConnectionTypes.Cellular, "Disconnect from WIFI and ensure cellular data")]
    [InlineData(ConnectionTypes.None, "Disconnect from WIFI and disable cellular data")]
    public async Task ConnectionTypeTests(ConnectionTypes type, string message)
    {
        var conn = this.GetService<IConnectivity>();
        await UserDialogs.Instance.AlertAsync(message + " & then press OK");
        conn.ConnectionTypes.HasFlag(type).Should().BeTrue();
    }


    [Fact(DisplayName = "Connectivity - Change Monitoring")]
    public async Task ChangeMonitorTest()
    {
        var conn = this.GetService<IConnectivity>();
        await UserDialogs.Instance.AlertAsync("Change anything with the connection to pass this test AFTER pressing OK");
        await conn
            .WhenChanged()
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(20))
            .ToTask();
    }
}
