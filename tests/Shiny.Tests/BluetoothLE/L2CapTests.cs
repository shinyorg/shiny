using System;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;

namespace Shiny.Tests.BluetoothLE;


[Trait("Category", "BluetoothLE")]
public class L2CapTests : AbstractBleTests
{
    public L2CapTests(ITestOutputHelper output) : base(output) { }


    [Fact(DisplayName = "BLE - L2Cap Host")]
    public async Task HostTest()
    {
        var service = this.GetService<IBleHostingManager>();
        var sub = service
            .WhenL2CapChannelOpened(false)
            .Subscribe(channel =>
            {
                //channel.Psm
            });
    }


    [Fact(DisplayName = "BLE - L2Cap Client")]
    public async Task ClientTest()
    {
        var service = this.GetService<IBleManager>();
        var peripheral = await service.Scan().Take(1).Select(x => x.Peripheral).ToTask();

        var sub = peripheral.TryOpenL2CapChannel(0, false)!.Subscribe(channel =>
        {

        });

        // TODO: need psm
    }
}

