//using System.Text;
//using Shiny.BluetoothLE;
//using Shiny.BluetoothLE.Hosting;

//namespace Shiny.Tests.BluetoothLE;


//[Trait("Category", "BluetoothLE")]
//public class L2CapTests : AbstractBleTests
//{
//    const string AD_SERVICE_UUID = "ee768769-1494-4690-860a-265de1d51bd5";

//    public L2CapTests(ITestOutputHelper output) : base(output) { }


//    [Fact(DisplayName = "BLE - L2Cap Host")]
//    public async Task HostTest()
//    {
//        L2CapInstance? instance = null;
//        try
//        {
//            var tcs = new TaskCompletionSource<bool>();
//            instance = await this.HostingManager.OpenL2Cap(false, async channel =>
//            {
//                try
//                {
//                    var buffer = new byte[8192];
//                    var read = await channel.InputStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
//                    if (read == -1)
//                        throw new Exception("No data read");

//                    await channel.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Hello!"));
//                    tcs.SetResult(true);
//                }
//                catch (Exception ex)
//                {
//                    tcs.SetException(ex);
//                }
//            });
//            await this.HostingManager.StartAdvertising(new(
//                "Tests",
//                AD_SERVICE_UUID
//            ));

//            await this.AlertWait($"PSM: {instance!.Value.Psm} - Waiting for a WRITE and then a READ", () => tcs.Task);
//        }
//        finally
//        {
//            instance?.Dispose();
//        }
//    }


//    [Fact(DisplayName = "BLE - L2Cap Client")]
//    public async Task ClientTest()
//    {
//        await this.FindFirstPeripheral(AD_SERVICE_UUID, true);
//        IDisposable? sub = null;

//        try
//        {
//            var tcs = new TaskCompletionSource<bool>();

//            var psmValue = await this.IntInput("PSM Value");
//            sub = this.Peripheral!.TryOpenL2CapChannel((ushort)psmValue, false)!.Subscribe(async channel =>
//            {
//                try
//                {
//                    var bytes = Encoding.UTF8.GetBytes("Hello!");
//                    channel.OutputStream.Write(bytes);
//                    //await channel.OutputStream.WriteAsync(bytes).ConfigureAwait(false);

//                    var buffer = new byte[8192];
//                    var read = await channel.InputStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
//                    if (read == -1)
//                        throw new Exception("Failed to read");

//                    tcs.SetResult(true);
//                }
//                catch (Exception ex)
//                {
//                    tcs.SetException(ex);
//                }
//            });
//            await tcs.Task;
//        }
//        finally
//        {
//            sub?.Dispose();
//        }
//    }
//}

