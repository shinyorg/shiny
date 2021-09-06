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


        [Fact]
        public async Task Peripherals_GetConnected()
        {
            var peripherals = await this.manager.GetConnectedPeripherals();

            foreach (var peripheral in peripherals)
            {
                this.output.WriteLine($"Connected Bluetooth Devices: Identifier={peripheral.Name} UUID={peripheral.Uuid} Connected={peripheral.IsConnected()}");
                peripheral.Status.Should().Be(ConnectionState.Connected);
            }
        }

#if __ANDROID__ || WINDOWS_UWP

        [Fact]
        public async Task Peripherals_GetPaired()
        {
            this.manager.CanViewPairedPeripherals().Should().BeTrue();

            var peripherals = await this.manager.TryGetPairedPeripherals();

            foreach (ICanPairPeripherals peripheral in peripherals)
            {
                this.output.WriteLine($"Paired Bluetooth Peripheral: Identifier={peripheral.Name} UUID={peripheral.Uuid} Paired={peripheral.PairingStatus}");
                peripheral.PairingStatus.Should().Be(PairingState.Paired);
            }
        }


        [Fact]
        public async Task Peripherals_GetKnown()
        {
            var peripherals = await this.manager.TryGetPairedPeripherals().ToTask().ConfigureAwait(false);

            // Get the first paired peripheral
            var known = peripherals.FirstOrDefault();
            if (known != null)
            {
                // Now try to get it from the known peripherals
                var found = await this.manager.GetKnownPeripheral(known.Uuid);
                known.Uuid.Should().Be(found.Uuid);
            }
            else
            {
                this.output.WriteLine("No well known peripheral found to test with. Please pair a peripheral");
            }
        }
#endif
    }
}
#endif