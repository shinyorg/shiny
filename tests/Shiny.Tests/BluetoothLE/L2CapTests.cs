using System;
using System.Text;
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
        var tcs = new TaskCompletionSource<bool>();
        var service = this.GetService<IBleHostingManager>();
        var instance = service.OpenL2Cap(false, channel =>
        {
            try
            {
                var buffer = new byte[8192];
                var read = channel.InputStream.Read(buffer);
                if (read == -1)
                    throw new Exception("No data read");

                channel.OutputStream.Write(Encoding.UTF8.GetBytes("Hello!"));
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        await this.AlertWait("Waiting for a WRITE and then a READ", () => tcs.Task);
    }


    [Fact(DisplayName = "BLE - L2Cap Client")]
    public async Task ClientTest()
    {
        var service = this.GetService<IBleManager>();
        var peripheral = await service.Scan().Take(1).Select(x => x.Peripheral).ToTask();
        var tcs = new TaskCompletionSource<bool>();

        var sub = peripheral.TryOpenL2CapChannel(0, false)!.Subscribe(channel =>
        {
            try
            {
                channel.OutputStream.Write(Encoding.UTF8.GetBytes("Hello!"));

                var buffer = new byte[8192];
                var read = channel.InputStream.Read(buffer, 0, buffer.Length);
                if (read == -1)
                    throw new Exception("Failed to read");

                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        await tcs.Task;
    }
}

