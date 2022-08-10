using Shiny.BluetoothLE;

namespace Shiny.Tests.BluetoothLE;


[Trait("Category", "BluetoothLE")]
public class BleManagerTests : AbstractBleTests
{
    public BleManagerTests(ITestOutputHelper output) : base(output) { }


    [Fact(DisplayName = "BLE Permissions")]
    public async Task Permissions()
    {
        var status = await this.GetService<IBleManager>().RequestAccess();
        status.Should().Be(AccessState.Available);
    }


    [Fact]
    public async Task Scan_Filter()
    {
        var result = await this.Manager
            .Scan(new ScanConfig(
                BleScanType.Balanced,
                false,
                this.Config.ServiceUuid
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
        var peripherals = await this.Manager.GetConnectedPeripherals();

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
        this.Manager.CanViewPairedPeripherals().Should().BeTrue();
        var peripherals = await this.Manager.TryGetPairedPeripherals();

        foreach (ICanPairPeripherals peripheral in peripherals)
        {
            this.Log($"Paired Bluetooth Peripheral: Identifier={peripheral.Name} UUID={peripheral.Uuid} Paired={peripheral.PairingStatus}");
            peripheral.PairingStatus.Should().Be(PairingState.Paired);
        }
    }


    [Fact]
    public async Task Android_Peripherals_GetKnown()
    {
        var peripherals = await this.Manager
            .TryGetPairedPeripherals()
            .ToTask()
            .ConfigureAwait(false);

        // get the first paired peripheral
        var known = peripherals.FirstOrDefault();
        if (known != null)
        {
            //Now try to get it from the known peripherals
            var found = await this.Manager.GetKnownPeripheral(known.Uuid);
            known.Uuid.Should().Be(found.Uuid);
        }
        else
        {
            this.Log("No well known peripheral found to test with. Please pair a peripheral");
        }
    }
#endif
}