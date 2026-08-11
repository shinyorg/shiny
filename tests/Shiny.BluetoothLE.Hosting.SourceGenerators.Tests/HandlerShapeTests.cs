using System.Linq;
using Xunit;

namespace Shiny.BluetoothLE.Hosting.SourceGenerators.Tests;


public class HandlerShapeTests
{
    [Fact]
    public void Read_ReturningBytes_WrapsInSuccess()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [ReadCharacteristic("2A37")]
                byte[] Read() => new byte[] { 1 };
            """));

        Assert.Empty(run.Ids());
        var source = run.Source("TestService.Service.g.cs");
        Assert.Contains("GattResult.Success(this.Read())", source);
        // a synchronous handler must not produce an async lambda with nothing to await
        Assert.DoesNotContain("async global::System.Threading.Tasks.Task<global::Shiny.BluetoothLE.Hosting.GattResult> __BleRead", source);
    }


    [Fact]
    public void Read_ReturningTaskOfBytes_Awaits()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [ReadCharacteristic("2A37")]
                Task<byte[]> Read() => Task.FromResult(new byte[] { 1 });
            """));

        Assert.Empty(run.Ids());
        Assert.Contains("GattResult.Success(await this.Read().ConfigureAwait(false))", run.AllSource);
    }


    [Fact]
    public void Read_ReturningGattResult_PassesThrough()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [ReadCharacteristic("2A37")]
                ValueTask<GattResult> Read() => new(GattResult.Error(GattState.ReadNotPermitted));
            """));

        Assert.Empty(run.Ids());
        Assert.Contains("return await this.Read().ConfigureAwait(false);", run.AllSource);
    }


    [Fact]
    public void Read_BindsEveryAvailableParameter()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [ReadCharacteristic("2A37", Encrypted = true)]
                byte[] Read(ReadRequest request, int offset, IPeripheral peripheral, TestServiceContext context, CancellationToken token)
                    => new byte[] { 1 };
            """));

        Assert.Empty(run.Ids());
        var source = run.AllSource;
        Assert.Contains("this.Read(request, request.Offset, request.Peripheral, this.GetContext(request.Peripheral), this.BleHostToken)", source);
        Assert.Contains("characteristic.SetRead(this.__BleRead_2A37, true);", source);
    }


    [Fact]
    public void Write_ReturningNothing_RespondsSuccessAndFailsOnThrow()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [WriteCharacteristic("2A39")]
                void Write(byte[] data) { }
            """));

        Assert.Empty(run.Ids());
        var source = run.AllSource;
        Assert.Contains("var status = global::Shiny.BluetoothLE.Hosting.GattState.Success;", source);
        Assert.Contains("status = global::Shiny.BluetoothLE.Hosting.GattState.Failure;", source);
        Assert.Contains("if (request.IsReplyNeeded) request.Respond(status);", source);
    }


    [Fact]
    public void Write_ReturningGattState_RespondsThatValue()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [WriteCharacteristic("2A39")]
                Task<GattState> Write(byte[] data) => Task.FromResult(GattState.InvalidOffset);
            """));

        Assert.Empty(run.Ids());
        Assert.Contains("status = await this.Write(request.Data).ConfigureAwait(false);", run.AllSource);
        Assert.Contains("if (request.IsReplyNeeded) request.Respond(status);", run.AllSource);
    }


    [Fact]
    public void Write_ManualRespond_LeavesRespondToTheHandler()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [WriteCharacteristic("2A39", ManualRespond = true)]
                Task Write(WriteRequest request)
                {
                    request.Respond(GattState.Success);
                    return Task.CompletedTask;
                }
            """));

        Assert.Empty(run.Ids());
        Assert.DoesNotContain("if (request.IsReplyNeeded) request.Respond(status);", run.AllSource);
        Assert.Contains("ManualRespond is set", run.AllSource);
    }


    [Fact]
    public void Write_WriteWithoutResponse_SelectsTheEnumValue()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [WriteCharacteristic("2A39", WriteWithoutResponse = true)]
                void Write(byte[] data) { }
            """));

        Assert.Empty(run.Ids());
        Assert.Contains("WriteOptions.WriteWithoutResponse", run.AllSource);
    }


    [Fact]
    public void Notify_OnMethod_GeneratesHookAndPushApi()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [NotifyCharacteristic("2A37", Indicate = true)]
                Task OnHeartRateAsync(BleSubscription subscription) => Task.CompletedTask;
            """));

        Assert.Empty(run.Ids());
        var source = run.AllSource;
        // On/Async trimmed off the hook name
        Assert.Contains("public global::System.Threading.Tasks.Task NotifyHeartRate(byte[] data", source);
        Assert.Contains("HeartRateSubscribers", source);
        Assert.Contains("public bool HasHeartRateSubscribers", source);
        Assert.Contains("characteristic.SetNotification(this.__BleSubscription_2A37, global::Shiny.BluetoothLE.Hosting.NotificationOptions.Indicate);", source);
    }


    [Fact]
    public void Notify_OnClass_GeneratesPushApiWithoutHook()
    {
        var run = GeneratorHarness.Run(Snippets.Service(
            """
                [ReadCharacteristic("2A19")]
                byte[] Read() => new byte[] { 100 };
            """,
            "[BleService(\"180F\")]\n[NotifyCharacteristic(\"2A19\", Name = \"BatteryLevel\")]"
        ));

        Assert.Empty(run.Ids());
        var source = run.AllSource;
        Assert.Contains("characteristic.SetNotification(null, global::Shiny.BluetoothLE.Hosting.NotificationOptions.Notify);", source);
        Assert.Contains("NotifyBatteryLevel(byte[] data", source);
        // read + notify on one UUID is the canonical battery shape and must stay legal
        Assert.Contains("characteristic.SetRead(this.__BleRead_2A19, false);", source);
    }


    [Fact]
    public void RequestResponse_RegistersWriteAndNotify_AndRepliesToTheWriter()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [RequestResponseCharacteristic("2A3B", Name = "Command")]
                Task<byte[]> Exchange(byte[] data) => Task.FromResult(data);
            """));

        Assert.Empty(run.Ids());
        var source = run.AllSource;
        Assert.Contains("characteristic.SetWrite(this.__BleWrite_2A3B", source);
        Assert.Contains("characteristic.SetNotification(null,", source);
        Assert.Contains("IsSubscribed(characteristic, request.Peripheral)", source);
        Assert.Contains("await characteristic!.Notify(result.Data, request.Peripheral)", source);
        Assert.Contains("this.OnBleResponseDropped(", source);
        Assert.Contains("NotifyCommand(byte[] data", source);
    }


    [Fact]
    public void Uuids_AreNormalizedToTheFull128BitForm()
    {
        // java.util.UUID.fromString on Android rejects short forms, so everything must be expanded
        var run = GeneratorHarness.Run(Snippets.Service("""
                [ReadCharacteristic("2A37")]
                byte[] Read() => new byte[] { 1 };
            """));

        Assert.Empty(run.Ids());
        Assert.Contains("\"00002A37-0000-1000-8000-00805F9B34FB\"", run.AllSource);
        Assert.Contains("\"0000180D-0000-1000-8000-00805F9B34FB\"", run.AllSource);
    }


    [Fact]
    public void Context_IsGeneratedAsAPartialDerivingFromBleServiceContext()
    {
        var run = GeneratorHarness.Run(Snippets.Service("""
                [ReadCharacteristic("2A37")]
                byte[] Read(TestServiceContext context) => new byte[] { 1 };
            """));

        Assert.Empty(run.Ids());
        var context = run.Source("TestService.Context.g.cs");
        Assert.Contains("public partial class TestServiceContext : global::Shiny.BluetoothLE.Hosting.BleServiceContext", context);
        Assert.Contains("BleContextStore.GetOrAdd(", run.Source("TestService.Service.g.cs"));
    }


    [Fact]
    public void Context_BindsEvenWhenTheUserDeclaredTheirOwnHalf()
    {
        // the other context tests deliberately omit the user's half, which is the harder case -
        // the parameter arrives as an error type because the generator has not emitted it yet
        var run = GeneratorHarness.Run(Snippets.Raw("""
            [BleService("180D")]
            public partial class TestService
            {
                [ReadCharacteristic("2A37")]
                byte[] Read(TestServiceContext context) => new byte[] { (byte)context.Mtu };
            }

            public partial class TestServiceContext
            {
                public string? User { get; set; }
            }
            """));

        Assert.Empty(run.Ids());
        Assert.Contains("this.Read(this.GetContext(request.Peripheral))", run.AllSource);
    }


    [Fact]
    public void L2Cap_EmitsListenerLifetimeAndPublishesThePsm()
    {
        var run = GeneratorHarness.Run(Snippets.Raw("""
            [BleService("180D")]
            public partial class GattService
            {
                [ReadCharacteristic("2A37")]
                byte[] Read() => new byte[] { 1 };
            }

            [L2CapService(Secure = true, PsmService = "180D", PsmCharacteristic = "2ABC", Name = "Echo")]
            public partial class StreamService
            {
                [OnChannelOpened]
                Task Serve(L2CapChannel channel, BleL2CapContext context, CancellationToken token) => Task.CompletedTask;
            }
            """));

        Assert.Empty(run.Ids());
        var l2cap = run.Source("StreamService.L2Cap.g.cs");
        Assert.Contains("public ushort Psm =>", l2cap);
        Assert.Contains(".OpenL2Cap(true, channel =>", l2cap);
        Assert.Contains("channel.Dispose();", l2cap);

        var registration = run.Source("BleHostedServiceRegistration.g.cs");
        Assert.Contains("EncodePsm(streamService.Psm)", registration);
        Assert.Contains("AddEcho(", registration);
        // listeners must come up before AddService, or an immediate PSM read returns zero
        Assert.True(
            registration.IndexOf("OpenL2Cap(manager)", System.StringComparison.Ordinal) <
            registration.IndexOf("manager.AddService(", System.StringComparison.Ordinal)
        );
    }
}
