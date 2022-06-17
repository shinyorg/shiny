using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xunit;
using Shiny.BluetoothLE;
using Xunit.Abstractions;
using Acr.UserDialogs;

namespace Shiny.Tests.BluetoothLE;


[Trait("Category", "BluetoothLE")]
public class PeripheralTests : AbstractBleTests
{
    public PeripheralTests(ITestOutputHelper output) : base(output) { }


    async Task Setup(bool connect)
    {
        this.Peripheral = await this.Manager
            .ScanUntilPeripheralFound(this.Config.PeripheralName)
            .Timeout(this.Config.DeviceScanTimeout)
            .ToTask();

        if (connect)
            await this.Connect();
    }


    Task Connect() => this.Peripheral!
        .WithConnectIf()
        .Timeout(this.Config.ConnectTimeout)
        .ToTask();


    [Fact]
    public async Task RssiTests()
    {
        await this.Setup(false);
        var obs = this.Peripheral!
            .ReadRssiContinuously()
            .Take(2)
            .Timeout(TimeSpan.FromSeconds(30));

        await this.Connect();
        await obs.ToTask();

        this.Peripheral!.CancelConnection();
        await this.Peripheral!.WhenDisconnected().Take(1).ToTask();

        await this.Connect();
        await obs.ToTask();
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


    //[Fact]
    //public async Task GetConnectedPeripherals()
    //{
    //    await this.Setup(true);
    //    var peripherals = await CrossBleAdapter.Current.GetConnectedPeripherals().ToTask();
    //    Assert.Equal(1, peripherals.Count());

    //    Assert.True(peripherals.First().Uuid.Equals(this.peripheral.Uuid));
    //    this.peripheral.CancelConnection();
    //    await Task.Delay(2000); // wait for dc to occur

    //    Assert.Equal(ConnectionStatus.Disconnected, this.peripheral.Status);
    //    peripherals = await CrossBleAdapter.Current.GetConnectedPeripherals().ToTask();
    //    Assert.Equal(0, peripherals.Count());
    //}


    [Fact]
    public async Task KnownCharacteristics_GetKnownCharacteristics_Consecutively()
    {
        await this.Setup(true);

        var s1 = await this.Peripheral!
            .GetKnownCharacteristic(this.Config.ServiceUuid, this.Config.WriteCharacteristicUuid)
            .Timeout(this.Config.OperationTimeout)
            .ToTask();

        var s2 = await this.Peripheral!
            .GetKnownCharacteristic(this.Config.ServiceUuid, this.Config.WriteCharacteristicUuid)
            .Timeout(this.Config.OperationTimeout)
            .ToTask();

        Assert.NotNull(s1);
        Assert.NotNull(s2);
    }


    //[Fact]
    //public async Task KnownCharacteristics_WhenKnownCharacteristic()
    //{
    //    await this.Setup(true);

    //    var tcs = new TaskCompletionSource<object>();
    //    var results = new List<IGattCharacteristic>();
    //    this.peripheral
    //        .WhenKnownCharacteristicDiscovered(
    //            Constants.ScratchServiceUuid,
    //            Constants.ScratchCharacteristicUuid1
    //        )
    //        .Subscribe(
    //            results.Add,
    //            ex => tcs.SetException(ex),
    //            () => tcs.SetResult(null)
    //        );

    //    await this.peripheral.ConnectWait();
    //    await Task.WhenAny(
    //        tcs.Task,
    //        Task.Delay(5000)
    //    );

    //    Assert.Equal(1, results.Count);
    //    Assert.True(results.Any(x => x.Uuid.Equals(Constants.ScratchCharacteristicUuid1)));
    //}


    [Fact]
    public async Task Extension_ReadWriteCharacteristic()
    {
        await this.Setup(false);

        await this.Peripheral!
            .WriteCharacteristic(
                this.Config.ServiceUuid,
                this.Config.WriteCharacteristicUuid,
                new byte[] { 0x01 }
            )
            .Timeout(this.Config.OperationTimeout)
            .ToTask();

        await this.Peripheral!
            .ReadCharacteristic(this.Config.ServiceUuid, this.Config.ReadCharacteristicUuid)
            .Timeout(this.Config.OperationTimeout)
            .ToTask();
    }


    [Fact]
    public async Task Extension_Notify()
    {
        await this.Setup(true);
        var list = await this.Peripheral!
            .Notify(
                this.Config.ServiceUuid,
                this.Config.NotifyCharacteristicUuid
            )
            .Take(3)
            .ToList()
            .Timeout(this.Config.OperationTimeout)
            .ToTask();

        Assert.Equal(3, list.Count);
    }


    [Fact]
    public async Task Notify_Reconnect()
    {
        await this.Setup(true);
        var count = 0;

        var sub = this.Peripheral!
            .Notify(this.Config.ServiceUuid, this.Config.NotifyCharacteristicUuid)
            .Subscribe(x => count++);

        await this.Peripheral!.WhenConnected().Take(1).ToTask();
        var disp = UserDialogs.Instance.Alert("Now turn off peripheral and wait");
        await this.Peripheral!.WhenDisconnected().Take(1).ToTask();
        count = 0;
        disp.Dispose();

        await Task.Delay(1000);
        disp = UserDialogs.Instance.Alert("Now turn peripheral on and wait");
        await this.Peripheral!.WhenConnected().Take(1).ToTask();
        disp.Dispose();

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
        await UserDialogs.Instance.AlertAsync("No turn peripheral off - wait a 3 seconds then turn it back on - press OK if light goes green or you believe connection has failed");
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
        await UserDialogs.Instance.AlertAsync("Now turn peripheral off & press OK");
        var origCount = count;
        count = 0;

        await UserDialogs.Instance.AlertAsync("Now turn peripheral back on & press OK when light turns green");
        await Task.Delay(5000);
        Assert.Equal(count, origCount);
    }
}
