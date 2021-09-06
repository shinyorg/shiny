#if DEVICE_TESTS
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Shiny.BluetoothLE;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using FluentAssertions;

namespace Shiny.Tests.BluetoothLE
{
    [Trait("Category", "BluetoothLE")]
    public class BleManagerTests
    {
        readonly IBleManager manager;
        readonly ITestOutputHelper output;


        public BleManagerTests(ITestOutputHelper output)
        {
            this.output = output;
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = x => x.UseBleClient(),
                BuildLogging = x => x.AddXUnit(output)
            });
            this.manager = ShinyHost.Resolve<IBleManager>();
        }



        [Fact]
        public async Task Scan_Filter()
        {
            var result = await this.manager
                .Scan(new ScanConfig
                {
                    ScanType = BleScanType.Balanced,
                    ServiceUuids =
                    {
                        Constants.AdServiceUuid
                    }
                })
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(20))
                .ToTask();

            Assert.NotNull(result);
            Assert.Equal("Bean+", result.Peripheral.Name);
        }


        //[Fact]
        //public async Task Scan_Extra_BackToBack()
        //{
        //    var ad =  Resolver.Resolve<ICentralManager>();

        //    var sub = ad.ScanExtra().Subscribe();

        //    Assert.True(ad.IsScanning);
        //    //await Task.Delay(2000);
        //    sub.Dispose();

        //    Assert.False(ad.IsScanning);
        //    sub = ad.ScanExtra(null, true).Subscribe();
        //    Assert.True(ad.IsScanning);

        //    sub.Dispose();
        //}


#if __ANDROID__
        [Fact]
        public async Task Devices_GetPaired()
        {
            this.manager.CanViewPairedPeripherals().Should().BeTrue();

            var peripherals = await this.manager.TryGetPairedPeripherals();

            foreach (ICanPairPeripherals peripheral in peripherals)
            {
                this.output.WriteLine($"Paired Bluetooth Devices: Identifier={peripheral.Name} UUID={peripheral.Uuid} Paired={peripheral.PairingStatus}");
                Assert.True(peripheral.PairingStatus == PairingState.Paired);
            }
        }
#endif


        //[Fact]
        //public async Task Devices_GetConnected()
        //{
        //    var devices = await this.manager.GetConnectedPeripherals();

        //    if (devices.Count() == 0)
        //    {
        //        this.output.WriteLine("There are no connected Bluetooth peripherals. Trying to connect a peripheral...");
        //        var paired = await this.manager.GetPairedPeripherals();

        //        // Get the first paired peripheral
        //        var device = paired.FirstOrDefault();
        //        if (device != null)
        //        {
        //            await device.ConnectWait().ToTask();
        //            devices = await this.manager.GetConnectedPeripherals();
        //        }
        //        else
        //        {
        //            this.output.WriteLine("There are no connected Bluetooth peripherals. Connect a peripheral and try again.");
        //        }
        //    }

        //    foreach (var device in devices)
        //    {
        //        this.output.WriteLine($"Connected Bluetooth Devices: Identifier={device.Name} UUID={device.Uuid} Connected={device.IsConnected()}");
        //        Assert.True(device.Status == ConnectionState.Connected);
        //    }
        //}


        //[Fact]
        //public async Task Devices_GetKnown()
        //{
        //    var devices = await this.manager.GetPairedPeripherals();

        //    // Get the first paired peripheral
        //    var known = devices.FirstOrDefault();
        //    if (known != null)
        //    {
        //        // Now try to get it from the known Devices
        //        var found = await this.manager.GetKnownPeripheral(known.Uuid);
        //        Assert.True(known.Uuid == found.Uuid);
        //    }
        //    else
        //    {
        //        this.output.WriteLine("No well known peripheral found to test with. Please pair a peripheral");
        //    }
        //}
    }
}
#endif