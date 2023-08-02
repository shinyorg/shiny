using System.IO;
using Shiny.BluetoothLE;

namespace Shiny.Tests.BluetoothLE;


[Trait("Category", "BLE Characteristics")]
public class CharacteristicTests : AbstractBleTests
{
    public CharacteristicTests(ITestOutputHelper output) : base(output) { }

    
    [Theory(DisplayName = "BLE Characteristic - Characteristic - Writes")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WriteTests(bool withResponse)
    {
        await this.Setup();
        var value = new byte[] { 0x01, 0x02 };

        var responseType = withResponse ? BleCharacteristicEvent.Write : BleCharacteristicEvent.WriteWithoutResponse;
        var result = await this.Peripheral!.WriteCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.WriteCharacteristicUuid, value, withResponse);
        result.Event.Should().Be(responseType);
        // result.Data.Should().Be(value);
    }


    [Fact(DisplayName = "BLE Characteristic - Find Multiple")]
    public async Task FindMultipleCharacteristicTest()
    {
        await this.Setup();

        var c1 = await this.Peripheral!.GetCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid);
        var c2 = await this.Peripheral!.GetCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid);

        c1.Should().NotBeNull("Read Characteristic should have been found");
        c2.Should().NotBeNull("Read Characteristic should have been found");
    }


    [Fact(DisplayName = "BLE Characteristic - Read")]
    public async Task ReadTests()
    {
        await this.Setup();
        var result = await this.Peripheral!.ReadCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid);
        result.Event.Should().Be(BleCharacteristicEvent.Read);
    }


    [Fact(DisplayName = "BLE Characteristic - Reconnect Read")]
    public async Task ReconnectReadTests()
    {
        await this.Setup();
        var result = await this.Peripheral!.ReadCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid);
        result.Event.Should().Be(BleCharacteristicEvent.Read);
        this.Log("Initial Read Complete - Moving to Reconnection");

        this.Peripheral!.CancelConnection();
        await Task.Delay(2000);
        await this.Connect();
        result = await this.Peripheral!.ReadCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid);
        result.Event.Should().Be(BleCharacteristicEvent.Read);
    }


    [Fact(DisplayName = "BLE Characteristic - Notification")]
    public async Task NotifyTest()
    {
        await this.Setup();
        await this.DoNotificationAndOut();
    }


    [Fact(DisplayName = "BLE Characteristic - Notification Disconnect and Rehook")]
    public async Task NotifyWithDisconnectAndRehook()
    {
        await this.Setup();

        this.Log("Doing initial notification hook");
        await this.DoNotificationAndOut();

        this.Peripheral!.CancelConnection();
        this.Log("Disconnected");

        await this.Connect();
        this.Log("Reconnected - Doing secondary notification hook");
        await this.DoNotificationAndOut();
    }


    [Fact(DisplayName = "BLE Characteristic - Multi-Notification")]
    public async Task MultipleNotifyTest()
    {
        await this.Setup();
        var task1 = this.Peripheral!
            .NotifyCharacteristic(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid)
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(30))
            .ToTask();

        var task2 = this.Peripheral!
            .NotifyCharacteristic(BleConfiguration.ServiceUuid, BleConfiguration.Notify2CharacteristicUuid)
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(30))
            .ToTask();

        await this.Peripheral!.WriteCharacteristicAsync(
            BleConfiguration.ServiceUuid,
            BleConfiguration.WriteCharacteristicUuid,
            new byte[] { 0x01 },
            true,
            CancellationToken.None,
            30000
        );

        await Task.WhenAll(task1, task2);
        task1.Result.Event.Should().Be(BleCharacteristicEvent.Notification);
        task2.Result.Event.Should().Be(BleCharacteristicEvent.Notification);
    }


    [Fact(DisplayName = "BLE Characteristic - Wait for Subscription Before")]
    public async Task WaitForSubscriptionBefore()
    {
        await this.Setup();

        var task = this.Peripheral!.WaitForCharacteristicSubscriptionAsync(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid);
        using var sub = this.Peripheral!
            .NotifyCharacteristic(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid, true)
            .Subscribe(x => this.Log("Notification Fired"));
        
        await task.WaitAsync(TimeSpan.FromSeconds(20));
    }
    
    
    [Fact(DisplayName = "BLE Characteristic - Wait for Subscription After")]
    public async Task WaitForSubscriptionAfter()
    {
        await this.Setup();

        using var sub = this.Peripheral!
            .NotifyCharacteristic(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid, true)
            .Subscribe(x => this.Log("Notification Fired"));

        await Task.Delay(8000); // let's wait for the hook internally
        await this.Peripheral!.WaitForCharacteristicSubscriptionAsync(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid).WaitAsync(TimeSpan.FromSeconds(3));
    }
    

    [Fact(DisplayName = "BLE Characteristic - Reconnect Notification")]
    public async Task ReconnectNotify()
    {
        await this.Setup();
        var notifyFired = new TaskCompletionSource();

        using var sub = this.Peripheral!
            .NotifyCharacteristic(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid, true)
            .Subscribe(x => notifyFired?.SetResult());

        // trigger first notification
        this.Log("Waiting for notify to be ready");
        await this.Peripheral!.WaitForCharacteristicSubscriptionAsync(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid);
        
        this.Log("Sending Write");
        await this.Peripheral!.WriteCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.WriteCharacteristicUuid, new byte[] { 0x02 }, true);
        
        this.Log("Waiting for notification to be fired");
        await notifyFired.Task.WaitAsync(TimeSpan.FromSeconds(3));
        
        this.Log("Initial Test Complete - Moving to reconnection");
        this.Peripheral!.CancelConnection(); // disconnecting will not remove notification, so we should expect a resubscription

        // reset completion sources
        notifyFired = new();
        await this.Connect();
        
        this.Log("RECONNECT - Waiting for notify to be ready");
        await this.Peripheral!.WaitForCharacteristicSubscriptionAsync(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid).WaitAsync(TimeSpan.FromSeconds(10));

        this.Log("RECONNECT - Sending Write");
        await this.Peripheral!.WriteCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.WriteCharacteristicUuid, new byte[] { 0x03 }, true);
        
        this.Log("RECONNECT - Waiting for notification to be fired");
        await notifyFired.Task.WaitAsync(TimeSpan.FromSeconds(3));
    }


    [Fact(DisplayName = "BLE Characteristic - Get All Characteristics")]
    public async Task GetAllCharacteristics()
    {
        await this.Setup();
        var results = await this.Peripheral!.GetAllCharacteristicsAsync();

        AssertChar(results, BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid);
        AssertChar(results, BleConfiguration.ServiceUuid, BleConfiguration.WriteCharacteristicUuid);
        AssertChar(results, BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid);
        AssertChar(results, BleConfiguration.ServiceUuid, BleConfiguration.Notify2CharacteristicUuid);
    }


    async Task DoNotificationAndOut()
    {
        this.Log("Will hook notification");
        var task = this.Peripheral!
            .NotifyCharacteristic(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid)
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(15))
            .ToTask();

        this.Log("Waiting for notify to be ready");
        await this.Peripheral!.WaitForCharacteristicSubscriptionAsync(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid);

        this.Log("Writing Request for notification");
        await this.Peripheral!.WriteCharacteristicAsync(
            BleConfiguration.ServiceUuid,
            BleConfiguration.WriteCharacteristicUuid,
            new byte[] { 0x01 },
            true,
            CancellationToken.None,
            30000
        );

        this.Log("Waiting for notification");
        var result = await task.ConfigureAwait(false);
        result.Event.Should().Be(BleCharacteristicEvent.Notification);
    }


    static void AssertChar(IReadOnlyList<BleCharacteristicInfo> results, string serviceUuid, string characteristicUuid)
        => results
            .FirstOrDefault(x =>
                x.Service.Uuid.Equals(serviceUuid, StringComparison.InvariantCultureIgnoreCase) &&
                x.Uuid.Equals(characteristicUuid, StringComparison.InvariantCultureIgnoreCase)
            )
            .Should()
            .NotBeNull($"Did not find service: {serviceUuid} / characteristic: {characteristicUuid}");


    [Fact(DisplayName = "BLE Characteristic - Blob Write")]
    public async Task BlobWrite()
    {
        await this.Setup();
        var tcs = new TaskCompletionSource();
        var filePath = Utils.GenerateFullFile(1);
        using var file = File.OpenRead(filePath);

        this.Peripheral!
            .WriteCharacteristicBlob(
                BleConfiguration.ServiceUuid,
                BleConfiguration.WriteCharacteristicUuid,
                file,
                TimeSpan.FromSeconds(5)
            )
            .Subscribe(
                x => this.Log($"Position: {x.Position} / {x.TotalLength} - Chunk Size: {x.Chunk.Length}"),
                ex => tcs.TrySetException(ex),
                () => tcs.TrySetResult()
            ) ;

        await tcs.Task.ConfigureAwait(false);
    }


    [Fact(DisplayName = "BLE Characteristic - Concurrent Writes")]
    public async Task Concurrent_Writes()
    {
        await this.Setup();
        var bytes = new byte[] { 0x01 };
        var list = new List<Task<BleCharacteristicResult>>();

        var ch = await this.Peripheral!.GetCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.WriteCharacteristicUuid);
        ch.Should().NotBeNull("Write characteristic was not found");
        
        for (var i = 0; i < 10; i++)
            list.Add(this.Peripheral!.WriteCharacteristicAsync(ch, bytes, true, CancellationToken.None, 5000));

        await Task.WhenAll(list);
    }


    [Fact(DisplayName = "BLE Characteristic - Concurrent Reads")]
    public async Task Concurrent_Reads()
    {
        await this.Setup();
        
        var ch = await this.Peripheral!.GetCharacteristicAsync(BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid);
        ch.Should().NotBeNull("Write characteristic was not found");
        
        var list = new List<Task<BleCharacteristicResult>>();
        for (var i = 0; i < 10; i++)
            list.Add(this.Peripheral!.ReadCharacteristicAsync(ch, CancellationToken.None, 5000));

        await Task.WhenAll(list);
    }


    [Fact(DisplayName = "BLE Characteristic - 16bit UUID")]
    public async Task UuidTest()
    {
        // this is built for testing against a BLE VEEPEAK dongle
        IPeripheral? peripheral = null;
        try
        {
            peripheral = await this.Manager
                .ScanUntilFirstPeripheralFound("FFF0")
                .Timeout(TimeSpan.FromSeconds(10))
                .ToTask();

            await peripheral.ConnectAsync().ConfigureAwait(false);

            var ch = await peripheral
                .GetCharacteristicAsync("FFF0", "FFF1")
                .ConfigureAwait(false);
        }
        finally
        {
            peripheral?.CancelConnection();
        }
    }
}
