using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;
using Shiny.Hosting;

namespace Shiny.Tests.BluetoothLE;


public abstract class AbstractBleTests : AbstractShinyTests
{
    protected AbstractBleTests(ITestOutputHelper output) : base(output) { }

    public override void Dispose()
    {
        this.Peripheral?.CancelConnection();
        this.HostingManager.DetachRegisteredServices();
        this.HostingManager.ClearServices();
        this.HostingManager.StopAdvertising();
        base.Dispose();
    }


    protected override void Configure(IHostBuilder hostBuilder)
    {
        hostBuilder.Services.AddBluetoothLE();
        hostBuilder.Services.AddBluetoothLeHosting();
    }


    protected IBleManager Manager => this.GetService<IBleManager>();
    protected IBleHostingManager HostingManager => this.GetService<IBleHostingManager>();
    protected Shiny.BluetoothLE.IPeripheral? Peripheral { get; set; }


    protected virtual async Task Setup(bool connect = true)
    {
        this.Peripheral = await this.Manager
            .ScanUntilFirstPeripheralFound(BleConfiguration.ServiceUuid)
            // .ScanUntilPeripheralFound("BleConfiguration.PeripheralName")
            .Timeout(BleConfiguration.DeviceScanTimeout)
            .ToTask();

        if (connect)
        {
            await this.Peripheral
                .WithConnectIf()
                .Timeout(BleConfiguration.ConnectTimeout) // android can take some time :P
                .ToTask();
        }
    }


    protected Task Connect() => this.Peripheral!
        .WithConnectIf()
        .Timeout(BleConfiguration.ConnectTimeout)
        .ToTask();


    protected virtual async Task FindFirstPeripheral(string serviceUuid, bool connect)
    {
        this.Log("Scanning for peripheral");
        this.Peripheral = await this.Manager
            .Scan(new(serviceUuid))
            .Select(x => x.Peripheral)
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(30))
            .ToTask();

        this.Log("Peripheral Found");

        if (connect)
        {
            this.Log("Connecting to peripheral");
            await this.Peripheral.ConnectAsync(timeout: TimeSpan.FromSeconds(5));
            this.Log("Connected to peripheral");
        }
    }
}
