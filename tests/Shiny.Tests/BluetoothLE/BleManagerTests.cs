using Shiny.BluetoothLE;

namespace Shiny.Tests.BluetoothLE;


[Trait("Category", "BLE Manager")]
public class BleManagerTests : AbstractBleTests
{
    public BleManagerTests(ITestOutputHelper output) : base(output) { }


    [Fact(DisplayName = "BLE Manager - Permissions")]
    public async Task Permissions()
    {
        var status = await this.GetService<IBleManager>().RequestAccess();
        status.Should().Be(AccessState.Available);
    }


    [Fact(DisplayName = "BLE Manager - Platform Scan Options")]
    public async Task Scan_Filter()
    {
        var result = await this.Manager
            .Scan(
#if ANDROID
                new AndroidScanConfig(
                    Android.Bluetooth.LE.ScanMode.Opportunistic,
                    false,
#else
                new ScanConfig(
#endif
                    BleConfiguration.ServiceUuid
                )
            )
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(20))
            .ToTask();

        result.Should().NotBeNull("Did not find a peripheral broadcasting unit test service UUID");
    }


    [Fact(DisplayName = "BLE Manager - Get Connected Peripherals")]
    public async Task Peripherals_GetConnected()
    {
        await this.Setup();
        var peripherals = this.Manager.GetConnectedPeripherals();

        foreach (var peripheral in peripherals)
        {
            this.Log($"Connected Bluetooth Devices: Identifier={peripheral.Name} UUID={peripheral.Uuid} Connected={peripheral.IsConnected()}");
            peripheral.Status.Should().Be(ConnectionState.Connected);
        }
    }

    [Fact(DisplayName = "BLE Manager - Request Access Async")]
    public async Task RequestAccessAsyncTest()
    {
        var result = await this.Manager.RequestAccessAsync();
        this.Log("Result: " + result);
    }

#if ANDROID

    [Fact(DisplayName = "BLE Manager - Android - Paired Peripherals")]
    public void Android_Peripherals_GetPaired()
    {
        this.Manager.CanViewPairedPeripherals().Should().BeTrue();
        var peripherals = this.Manager.TryGetPairedPeripherals();

        foreach (ICanPairPeripherals peripheral in peripherals)
        {
            this.Log($"Paired Bluetooth Peripheral: Identifier={peripheral.Name} UUID={peripheral.Uuid} Paired={peripheral.PairingStatus}");
            peripheral.PairingStatus.Should().Be(PairingState.Paired);
        }
    }
#endif
}