using Shiny.BluetoothLE;

namespace Shiny.Tests.BluetoothLE;


[Trait("Category", "BluetoothLE")]
public class PeripheralTests : AbstractBleTests
{
    public PeripheralTests(ITestOutputHelper output) : base(output) { }


    [Fact(DisplayName = "BLE Client - Peripheral Status")]
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
                // TODO: could test startsWith by making it count connects
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
        
        // TODO: send write for disconnect, wait 15 seconds for readvertise & connect?

        this.Log("Disconnecting");
        this.Peripheral!.CancelConnection(); // auto reconnect will be cancelled here, but event will still fire
        await Task.Delay(5000);
        
        this.Log("Reconnecting");
        await this.Connect();

        connected.Should().Be(true, "Connected should be true");
        disconnected.Should().Be(true, "Disconnected should be true");
    }
    
    
    [Fact(DisplayName = "BLE Client - RSSI")]
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


    [Fact(DisplayName = "BLE Client - MTU Request")]
    public async Task MtuRequestTest()
    {
        await this.Setup();
        var mtu = await this.Peripheral!.TryRequestMtuAsync(512);
        this.Log("MTU: " + mtu);
    }
    //[Fact]
    //public async Task Service_Rediscover()
    //{
    //    await this.Setup(true);
    //    var services1 = await this.peripheral
    //        .GetCharacteristicsForService(Constants.ScratchServiceUuid)
    //        .Timeout(Constants.OperationTimeout)
    //        .ToList()
    //        .ToTask();

    //    var services2 = await this.peripheral
    //        .GetCharacteristicsForService(Constants.ScratchServiceUuid)
    //        .Timeout(Constants.OperationTimeout)
    //        .ToList()
    //        .ToTask();

    //    Assert.Equal(services1.Count, services2.Count);
    //}


    [Fact]
    public async Task Notify_Reconnect()
    {
        await this.Setup(true);
        var count = 0;

        var sub = this.Peripheral!
            .NotifyCharacteristic(
                BleConfiguration.ServiceUuid,
                BleConfiguration.NotifyCharacteristicUuid
            )
            .Subscribe(x => count++);

        await this.Peripheral!.WhenConnected().Take(1).ToTask();
        await this.AlertWait(
            "Now turn off peripheral and wait",
            () => this.Peripheral!.WhenDisconnected().Take(1).ToTask()
        );
        count = 0;

        await Task.Delay(1000);
        await this.AlertWait(
            "Now turn peripheral on and wait",
            () => this.Peripheral!.WhenConnected().Take(1).ToTask()
        );

        await Task.Delay(3000);
        sub.Dispose();
        Assert.True(count > 0, "No pings");
    }


    [Fact]
    public async Task ReconnectTest()
    {
        var connected = 0;
        var disconnected = 0;

        await this.Setup(false);
        this.Peripheral!
            .WhenStatusChanged()
            .Subscribe(x =>
            {
                switch (x)
                {
                    case ConnectionState.Disconnected:
                        disconnected++;
                        break;

                    case ConnectionState.Connected:
                        connected++;
                        break;
                }
            });

        await this.Peripheral!.WithConnectIf();
        await this.Alert("No turn peripheral off - wait a 3 seconds then turn it back on - press OK if light goes green or you believe connection has failed");
        Assert.Equal(2, connected);
        Assert.Equal(2, disconnected);
    }


    [Fact]
    public async Task CancelConnection_And_Watch()
    {
        await this.Setup(true);
        this.Peripheral!.CancelConnection();
        await this.Peripheral!.WhenDisconnected().Timeout(TimeSpan.FromSeconds(5)).Take(1).ToTask();
    }


    [Fact]
    public async Task Reconnect_WhenServiceFound_ShouldFlushOriginals()
    {
        await this.Setup(false);

        var count = 0;
        this.Peripheral!.GetServices().Subscribe(_ => count++);

        await this.Peripheral!.WithConnectIf().ToTask();
        await this.Alert("Now turn peripheral off & press OK");
        var origCount = count;
        count = 0;

        await this.Alert("Now turn peripheral back on & press OK when light turns green");
        await Task.Delay(5000);
        Assert.Equal(count, origCount);
    }
}