using Shiny.BluetoothLE;

namespace Shiny.Tests.BluetoothLE;


[Trait("Category", "BLE Services")]
public class ServiceTests : AbstractBleTests
{
    public ServiceTests(ITestOutputHelper output) : base(output)
    {
    }


    [Fact(DisplayName = "BLE Services - Discover Specific Then All")]
    public async Task DiscoverSpecificAndThenAll()
    {
        await this.Setup();
        await this.Peripheral!.GetServiceAsync(BleConfiguration.ServiceUuid);

        var services = await this.Peripheral!.GetServicesAsync();
        services.Count.Should().BeGreaterThan(1);
    }


    [Fact(DisplayName = "BLE Services - Disconnect & Rediscover")]
    public async Task DisconnectAndRediscover()
    {
        await this.Setup();
        await this.Peripheral!.GetServiceAsync(BleConfiguration.ServiceUuid);

        this.Log("Disconnecting");
        this.Peripheral!.CancelConnection();

        this.Log("Reconnecting");
        await this.Connect();
        await this.Peripheral!.GetServiceAsync(BleConfiguration.ServiceUuid);
    }
}