using System.Reactive.Subjects;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;
using Shiny.BluetoothLE.Hosting.Managed;
using Shiny.Hosting;

namespace Shiny.Tests.BluetoothLE;


public class BleHostTests : AbstractBleTests
{
    public BleHostTests(ITestOutputHelper output) : base(output) { }


    protected override void Configure(HostBuilder hostBuilder)
    {
        base.Configure(hostBuilder);
        hostBuilder.Services.AddBluetoothLeHosting();

        hostBuilder.Services.AddBleHostedCharacteristic<ManagedTestCharacteristicReadOnly>();
        hostBuilder.Services.AddBleHostedCharacteristic<ManagedTestCharacteristicWriteOnly>();
        hostBuilder.Services.AddBleHostedCharacteristic<ManagedTestCharacteristicSubOnly>();
    }


    public override void Dispose()
    {
        ManagedTestCharacteristicReadOnly.Callback = null;
        ManagedTestCharacteristicWriteOnly.Callback = null;
        ManagedTestCharacteristicSubOnly.Callback = null;
    }


    //[Fact(DisplayName = "Beacon Advertising")]
    //public async Task BeaconAdvertising()
    //{
    //    await this.GetService<IBleHostingManager>().AdvertiseBeacon(
    //        Guid.NewGuid(),
    //        11,
    //        222,
    //        -1
    //    );
    //}


    [Fact(DisplayName = "BLE Host - Client Tester")]
    public async Task BleHostClientTester()
    {
        await this.FindFirstPeripheral(BleConfiguration.ServiceUuid, false);

        await this.Peripheral!.WriteCharacteristicAsync(
            BleConfiguration.ServiceUuid,
            BleConfiguration.WriteCharacteristicUuid,
            new byte[] { 0x01 }
        );

        await this.Peripheral!.ReadCharacteristicAsync(
            BleConfiguration.ServiceUuid,
            BleConfiguration.ReadCharacteristicUuid
        );

        await this.Peripheral!
            .NotifyCharacteristic(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid)
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(5))
            .ToTask();
    }


    [Fact(DisplayName = "BLE Host - End-To-End")]
    public async Task BleHostEndToEnd()
    {
        var subj = new Subject<bool>();
        var bleHost = this.HostingManager;

        await bleHost.AddService(BleConfiguration.ServiceUuid, true, sb =>
        {
            var ch = sb.AddCharacteristic(
                BleConfiguration.NotifyCharacteristicUuid,
                cb => cb
                    .SetNotification(async x =>
                    {
                        this.Log("Subscription Received");
                        subj.OnNext(true);
                        try
                        {
                            await x.Characteristic.Notify(new byte[] { 0x02 });
                            this.Log("Sent Notification");
                        }
                        catch (Exception ex)
                        {
                            this.Log("Failed to notify: " + ex);
                        }
                    })
                    .SetRead(x =>
                    {
                        this.Log("Read Received");
                        subj.OnNext(true);
                        return Task.FromResult(GattResult.Success(new byte[] { 0x0 }));
                    })
                    .SetWrite(x =>
                    {
                        this.Log("Write Received");
                        subj.OnNext(true);
                        return Task.FromResult(GattState.Success);
                    })
            );
        });

        await bleHost.StartAdvertising(new AdvertisementOptions(
            "Tests",
            BleConfiguration.ServiceUuid
        ));

        await subj
            .Take(3)
            //.Timeout(TimeSpan.FromSeconds(20))
            .ToTask();
    }


    [Fact(DisplayName = "BLE Host - Managed End-To-End")]
    public async Task ManagedEndToEnd()
    {
        var subj = new Subject<bool>();
        var bleHost = this.HostingManager;

        ManagedTestCharacteristicReadOnly.Callback = async () =>
        {
            this.Log("Read Received");
            subj.OnNext(true);
        };
        ManagedTestCharacteristicWriteOnly.Callback = async () =>
        {
            this.Log("Write Received");
            subj.OnNext(true);
        };
        ManagedTestCharacteristicSubOnly.Callback = async x =>
        {
            subj.OnNext(true);
            this.Log("Sub Received");
            await x.Notify(new byte[] { 0x02 });
        };

        bleHost.ClearServices();
        try
        {
            await bleHost.AttachRegisteredServices();
            this.ValidateCharacteristicHooks();

            await subj
                .Take(3)
                //.Timeout(TimeSpan.FromSeconds(20))
                .ToTask();
        }
        finally
        {
            bleHost.DetachRegisteredServices();
        }
    }


    void ValidateCharacteristicHooks()
    {
        var chs = this.Host.Services.GetServices<BleGattCharacteristic>();

        // TODO: I have to start managed services
        this.AssertChr(chs, BleConfiguration.ReadCharacteristicUuid, CharacteristicProperties.Read);
        this.AssertChr(chs, BleConfiguration.WriteCharacteristicUuid, CharacteristicProperties.Write);
        this.AssertChr(chs, BleConfiguration.NotifyCharacteristicUuid, CharacteristicProperties.Notify);
    }


    void AssertChr(IEnumerable<BleGattCharacteristic> chs, string characteristicUuid, CharacteristicProperties prop)
    {
        var ch = chs.FirstOrDefault(x => x.Characteristic.Uuid.Equals(characteristicUuid));
        ch.Should().NotBeNull($"Characteristic '{characteristicUuid}' not found");

        // TODO: props isn't casting properly
        //var p = ch!.Characteristic.Properties;
        //p.Should().Be(prop);
    }
}


[BleGattCharacteristic(BleConfiguration.ServiceUuid, BleConfiguration.ReadCharacteristicUuid)]
public class ManagedTestCharacteristicReadOnly : BleGattCharacteristic
{
    public static Func<Task>? Callback { get; set; }


    public override async Task<GattResult> OnRead(ReadRequest request)
    {
        if (Callback != null)
            await Callback.Invoke();

        return GattResult.Success(new byte[] { 0x01 });
    }
}


[BleGattCharacteristic(BleConfiguration.ServiceUuid, BleConfiguration.WriteCharacteristicUuid)]
public class ManagedTestCharacteristicWriteOnly : BleGattCharacteristic
{
    public static Func<Task>? Callback { get; set; }

    public override async Task<GattState> OnWrite(WriteRequest request)
    {
        if (Callback != null)
            await Callback.Invoke();

        return GattState.Success;
    }
}

[BleGattCharacteristic(BleConfiguration.ServiceUuid, BleConfiguration.NotifyCharacteristicUuid)]
public class ManagedTestCharacteristicSubOnly : BleGattCharacteristic
{
    public static Func<Shiny.BluetoothLE.Hosting.IGattCharacteristic, Task>? Callback { get; set; }

    public override async Task OnSubscriptionChanged(Shiny.BluetoothLE.Hosting.IPeripheral peripheral, bool subscribed)
    {
        if (Callback != null)
            await Callback.Invoke(this.Characteristic);
    }
}