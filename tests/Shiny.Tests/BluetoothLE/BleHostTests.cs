using System;
using System.Reactive.Subjects;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;
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
    }


    [Fact(DisplayName = "BLE Host Client Tester")]
    public async Task BleHostClientTester()
    {
        this.Peripheral = await this.Manager
            .Scan(new ScanConfig(
                ServiceUuids: SERVICE_UUID
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


    [Fact(DisplayName = "BLE Host End-To-End")]
    public async Task BleHostEndToEnd()
    {
        var subj = new Subject<bool>();
        var bleHost = this.GetService<IBleHostingManager>();

        await bleHost.AddService(SERVICE_UUID, false, sb =>
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
                        return ReadResult.Success(new byte[] { 0x0 });
                    })
                    .SetWrite(x =>
                    {
                        this.Log("Write Received");
                        subj.OnNext(true);
                        return GattState.Success;
                    })
            );
        });

        await bleHost.StartAdvertising(new AdvertisementOptions
        {
            AndroidIncludeDeviceName = true,
            ServiceUuids =
            {
                SERVICE_UUID
            }
        });

        await subj
            .Take(3)
            .Timeout(TimeSpan.FromSeconds(20))
            .ToTask();
    }
}