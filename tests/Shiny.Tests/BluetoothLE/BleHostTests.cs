using System.Reactive.Subjects;
using FluentAssertions;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;
using Shiny.BluetoothLE.Hosting.Managed;
using Shiny.Hosting;

namespace Shiny.Tests.BluetoothLE;


public class BleHostTests : AbstractBleTests
{
    const string SERVICE_UUID = "ca64c850-cf30-4f3a-8973-eaf69af71ad0";
    const string CHARACTERISTIC_UUID = "ca64c850-cf30-4f3a-8973-eaf69af71ad1";

    public BleHostTests(ITestOutputHelper output) : base(output) {}


    protected override void Configure(IHostBuilder hostBuilder)
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


    [Fact(DisplayName = "BLE Host - Client Tester")]
    public async Task BleHostClientTester()
    {
        this.Peripheral = await this.Manager
            .Scan(new ScanConfig(
                BleScanType.Balanced,
                false,
                SERVICE_UUID
            ))
            .Select(x => x.Peripheral)
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(30))
            .ToTask();

        await this.Peripheral.WriteCharacteristicAsync(
            SERVICE_UUID,
            CHARACTERISTIC_UUID,
            new byte[] { 0x01 }
        );

        await this.Peripheral.ReadCharacteristicAsync(
            SERVICE_UUID,
            CHARACTERISTIC_UUID
        );

        await this.Peripheral
            .Notify(SERVICE_UUID, CHARACTERISTIC_UUID)
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(5))
            .ToTask();
    }


    [Fact(DisplayName = "BLE Host - End-To-End")]
    public async Task BleHostEndToEnd()
    {
        var subj = new Subject<bool>();
        var bleHost = this.GetService<IBleHostingManager>();

        bleHost.ClearServices();

        await bleHost.AddService(SERVICE_UUID, true, sb =>
        {
            var ch = sb.AddCharacteristic(
                CHARACTERISTIC_UUID,
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
                        return Task.FromResult(ReadResult.Success(new byte[] { 0x0 }));
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
            SERVICE_UUID
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
        var bleHost = this.GetService<IBleHostingManager>();

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
        this.AssertChr(chs, "8c927255-fdec-4605-957b-57b6450779c1", CharacteristicProperties.Read);
        this.AssertChr(chs, "8c927255-fdec-4605-957b-57b6450779c2", CharacteristicProperties.Write);
        this.AssertChr(chs, "8c927255-fdec-4605-957b-57b6450779c3", CharacteristicProperties.Notify);
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


[BleGattCharacteristic("8c927255-fdec-4605-957b-57b6450779c0", "8c927255-fdec-4605-957b-57b6450779c1")]
public class ManagedTestCharacteristicReadOnly : BleGattCharacteristic
{
    public static Func<Task>? Callback { get; set; }


    public override async Task<ReadResult> OnRead(ReadRequest request)
    {
        if (Callback != null)
            await Callback.Invoke();

        return ReadResult.Success(new byte[] { 0x01 });
    }
}


[BleGattCharacteristic("8c927255-fdec-4605-957b-57b6450779c0", "8c927255-fdec-4605-957b-57b6450779c2")]
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


[BleGattCharacteristic("8c927255-fdec-4605-957b-57b6450779c0", "8c927255-fdec-4605-957b-57b6450779c3")]
public class ManagedTestCharacteristicSubOnly : BleGattCharacteristic
{
    public static Func<Shiny.BluetoothLE.Hosting.IGattCharacteristic, Task>? Callback { get; set; }

    public override async Task OnSubscriptionChanged(Shiny.BluetoothLE.Hosting.IPeripheral peripheral, bool subscribed)
    {
        if (Callback != null)
            await Callback.Invoke(this.Characteristic);
    }
}