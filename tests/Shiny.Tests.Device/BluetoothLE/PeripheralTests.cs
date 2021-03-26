using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Acr.UserDialogs;
using Shiny.BluetoothLE;


namespace Shiny.Tests.BluetoothLE
{
    [Trait("Category", "BluetoothLE")]
    public class PeripheralTests : IDisposable
    {
        readonly IBleManager manager;
        IPeripheral peripheral;


        public PeripheralTests()
        {
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = x => x.UseBleClient()
            });
            this.manager = ShinyHost.Resolve<IBleManager>();
        }


        public void Dispose()
        {
            this.peripheral?.CancelConnection();
        }


        async Task Setup(bool connect)
        {
            this.peripheral = await this.manager
                .ScanUntilPeripheralFound(Constants.PeripheralName)
                .Timeout(Constants.DeviceScanTimeout)
                .ToTask();

            if (connect)
                await this.Connect();
        }


        Task Connect() => this.peripheral
            .WithConnectIf()
            .Timeout(Constants.ConnectTimeout)
            .ToTask();


        [Fact]
        public async Task RssiTests()
        {
            await this.Setup(false);
            var obs = this.peripheral
                .ReadRssiContinuously()
                .Take(2)
                .Timeout(TimeSpan.FromSeconds(30));

            await this.Connect();
            await obs.ToTask();

            this.peripheral.CancelConnection();
            await this.peripheral.WhenDisconnected().Take(1).ToTask();

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

            var s1 = await this.peripheral
                .GetKnownCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid1)
                .Timeout(Constants.OperationTimeout)
                .ToTask();

            var s2 = await this.peripheral
                .GetKnownCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid2)
                .Timeout(Constants.OperationTimeout)
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

            await this.peripheral
                .WriteCharacteristic(
                    Constants.ScratchServiceUuid,
                    Constants.ScratchCharacteristicUuid1,
                    new byte[] { 0x01 }
                )
                .Timeout(Constants.OperationTimeout)
                .ToTask();

            await this.peripheral
                .ReadCharacteristic(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid1)
                .Timeout(Constants.OperationTimeout)
                .ToTask();
        }


        [Fact]
        public async Task Extension_Notify()
        {
            await this.Setup(true);
            var list = await this.peripheral
                .Notify(
                    Constants.ScratchServiceUuid,
                    Constants.ScratchCharacteristicUuid1
                )
                .Take(3)
                .ToList()
                .Timeout(Constants.OperationTimeout)
                .ToTask();

            Assert.Equal(3, list.Count);
        }


        [Fact]
        public async Task Notify_Reconnect()
        {
            await this.Setup(true);
            var count = 0;

            var sub = this.peripheral
                .Notify(Constants.ScratchServiceUuid, Constants.ScratchCharacteristicUuid1)
                .Subscribe(x => count++);

            await this.peripheral.WhenConnected().Take(1).ToTask();
            var disp = UserDialogs.Instance.Alert("Now turn off peripheral and wait");
            await this.peripheral.WhenDisconnected().Take(1).ToTask();
            count = 0;
            disp.Dispose();

            await Task.Delay(1000);
            disp = UserDialogs.Instance.Alert("Now turn peripheral on and wait");
            await this.peripheral.WhenConnected().Take(1).ToTask();
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
            this.peripheral
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

            await this.peripheral.WithConnectIf();
            await UserDialogs.Instance.AlertAsync("No turn peripheral off - wait a 3 seconds then turn it back on - press OK if light goes green or you believe connection has failed");
            Assert.Equal(2, connected);
            Assert.Equal(2, disconnected);
        }


        [Fact]
        public async Task CancelConnection_And_Watch()
        {
            await this.Setup(true);
            this.peripheral.CancelConnection();
            await this.peripheral.WhenDisconnected().Timeout(TimeSpan.FromSeconds(5)).Take(1).ToTask();
        }

        [Fact]
        public async Task Reconnect_WhenServiceFound_ShouldFlushOriginals()
        {
            await this.Setup(false);

            var count = 0;
            this.peripheral.GetServices().Subscribe(_ => count++);

            await this.peripheral.WithConnectIf().ToTask();
            await UserDialogs.Instance.AlertAsync("Now turn peripheral off & press OK");
            var origCount = count;
            count = 0;

            await UserDialogs.Instance.AlertAsync("Now turn peripheral back on & press OK when light turns green");
            await Task.Delay(5000);
            Assert.Equal(count, origCount);
        }
    }
}
