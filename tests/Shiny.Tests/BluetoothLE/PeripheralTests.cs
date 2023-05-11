using Shiny.BluetoothLE;

namespace Shiny.Tests.BluetoothLE;


[Trait("Category", "BLE Peripheral")]
public class PeripheralTests : AbstractBleTests
{
    public PeripheralTests(ITestOutputHelper output) : base(output) { }


    [Fact(DisplayName = "BLE Peripheral - Status Checks")]
    public async Task WhenStatusChangedTests()
    {
        var connected = false;
        var disconnected = false;
        
        await this.Setup(true);
        var sub = this.Peripheral!
            .WhenStatusChanged()
            .Skip(1)
            .Subscribe(state =>
            {
                this.Log("State Changed: " + state);
                switch (state)
                {
                    case ConnectionState.Connected:
                        connected = true;
                        break;
                    
                    case ConnectionState.Disconnected:
                        disconnected = true;
                        break;
                }
            })
            .DisposedBy(this.Disposable);

        this.Log("Disconnecting");
        this.Peripheral!.CancelConnection(); // auto reconnect will be cancelled here, but event will still fire
        await Task.Delay(5000);
        
        this.Log("Reconnecting");
        await this.Connect();

        connected.Should().Be(true, "Connected should be true");
        disconnected.Should().Be(true, "Disconnected should be true");
    }
    
    
    [Fact(DisplayName = "BLE Peripheral - RSSI")]
    public async Task RssiTests()
    {
        await this.Setup(false);
        var obs = this.Peripheral!
            .ReadRssi()
            .Take(2)
            .Timeout(TimeSpan.FromSeconds(30));

        await this.Connect();
        await obs.ToTask();

        this.Peripheral!.CancelConnection();
        await this.Peripheral!.WhenDisconnected().Take(1).ToTask();

        await this.Connect();
        await obs.ToTask();
    }


    [Fact(DisplayName = "BLE Peripheral - MTU Request")]
    public async Task MtuRequestTest()
    {
        await this.Setup();
        var mtu = await this.Peripheral!.TryRequestMtuAsync(512);
        this.Log("MTU: " + mtu);
    }
}