using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Shiny.BluetoothLE;
using Dialogs = Acr.UserDialogs.UserDialogs;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Logging;


namespace Shiny.Tests.BluetoothLE
{
    [Trait("Category", "BluetoothLE")]
    public class BleManagerTests
    {
        readonly IBleManager manager;


        public BleManagerTests(ITestOutputHelper output)
        {
            ShinyHost.Init(TestStartup.CurrentPlatform, new ActionStartup
            {
                BuildServices = x => x.UseBleClient(),
                BuildLogging = x => x.AddXUnit(output)
            });
            this.manager = ShinyHost.Resolve<IBleManager>();
        }


        [Fact]
        public async Task Status_Monitor()
        {
            var on = 0;
            var off = 0;
            this.manager
                .WhenStatusChanged()
                .Skip(1) // skip startwith
                .Subscribe(x =>
                {
                    switch (x)
                    {
                        case AccessState.Available:
                            on++;
                            break;

                        case AccessState.Disabled:
                            off++;
                            break;
                    }
                });
            await Dialogs.Instance.AlertAsync("Now turn the adapter off and then back on - press ok once done");

            Assert.True(on >= 1);
            Assert.True(off >= 1);
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


        //[Fact]
        //public async Task Devices_GetPaired()
        //{
        //    var devices = await this.manager.GetPairedPeripherals();

        //    foreach (var device in devices)
        //    {
        //        this.output.WriteLine($"Paired Bluetooth Devices: Identifier={device.Name} UUID={device.Uuid} Paired={device.PairingStatus}");
        //        Assert.True(device.PairingStatus == PairingState.Paired);
        //    }
        //}


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