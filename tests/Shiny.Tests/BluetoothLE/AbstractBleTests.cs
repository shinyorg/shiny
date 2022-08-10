using Microsoft.Extensions.Configuration;
using Shiny.BluetoothLE;
using Shiny.Hosting;

namespace Shiny.Tests.BluetoothLE;


public abstract class AbstractBleTests : AbstractShinyTests
{
    protected AbstractBleTests(ITestOutputHelper output) : base(output)
    {
        MauiProgram.Configuration.GetSection("BlueoothLE").Get<BleConfiguration>();
    }


    public override void Dispose()
    {
        this.Peripheral?.CancelConnection();
        base.Dispose();
    }

    protected override void Configure(IHostBuilder hostBuilder) => hostBuilder.Services.AddBluetoothLE();
    protected IBleManager Manager => this.GetService<IBleManager>();
    protected IPeripheral? Peripheral { get; set; }
    protected BleConfiguration Config { get; }
}
