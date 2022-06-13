using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;
using Xunit;
using Xunit.Abstractions;
using FluentAssertions;
using Shiny.Hosting;

namespace Shiny.Tests.BluetoothLE;


[Trait("Category", "BluetoothLE")]
public class BleManagerTests : AbstractShinyTests
{
    public BleManagerTests(ITestOutputHelper output) : base(output) { }
    protected override void Configure(IHostBuilder hostBuilder) => hostBuilder.Services.AddBluetoothLE();


    [Fact]
    public async Task Scan_Filter()
    {
        var result = await this.GetService<IBleManager>()
            .Scan(new ScanConfig(
                BleScanType.Balanced,
                false,
                Constants.AdServiceUuid
            ))
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(20))
            .ToTask();

        Assert.NotNull(result);
        Assert.Equal("Bean+", result.Peripheral.Name);
    }


    [Fact]
    public async Task Peripherals_GetConnected()
    {
        var peripherals = await this.GetService<IBleManager>().GetConnectedPeripherals();

        foreach (var peripheral in peripherals)
        {
            this.Log($"Connected Bluetooth Devices: Identifier={peripheral.Name} UUID={peripheral.Uuid} Connected={peripheral.IsConnected()}");
            peripheral.Status.Should().Be(ConnectionState.Connected);
        }
    }

#if ANDROID

    [Fact]
    public async Task Android_Peripherals_GetPaired()
    {
        var manager = this.GetService<IBleManager>();
        manager.CanViewPairedPeripherals().Should().BeTrue();

        var peripherals = await manager.TryGetPairedPeripherals();

        foreach (ICanPairPeripherals peripheral in peripherals)
        {
            this.Log($"Paired Bluetooth Peripheral: Identifier={peripheral.Name} UUID={peripheral.Uuid} Paired={peripheral.PairingStatus}");
            peripheral.PairingStatus.Should().Be(PairingState.Paired);
        }
    }


    [Fact]
    public async Task Android_Peripherals_GetKnown()
    {
        var manager = this.GetService<IBleManager>();
        var peripherals = await manager.TryGetPairedPeripherals().ToTask().ConfigureAwait(false);

        // get the first paired peripheral
        var known = peripherals.FirstOrDefault();
        if (known != null)
        {
             //Now try to get it from the known peripherals
            var found = await manager.GetKnownPeripheral(known.Uuid);
            known.Uuid.Should().Be(found.Uuid);
        }
        else
        {
            this.Log("No well known peripheral found to test with. Please pair a peripheral");
        }
    }
#endif
}