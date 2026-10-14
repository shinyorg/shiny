using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;

namespace Shiny.DeviceTests.Tests;


/// <summary>
/// Single-device BLE checks - no peer needed: access, a scan starting and stopping cleanly, and the hosting
/// manager's advertising/service lifecycle and documented platform limits.
/// </summary>
public class BleLocalTests(IBleManager bleManager, IBleHostingManager hosting) : DeviceTestSuite
{
    public override string Name => "BleLocal";


    public override Task SetupAsync(TestContext context)
    {
        context.RequireBluetooth();
        return Task.CompletedTask;
    }


    protected override void Define()
    {
        this.Test("Central_AccessIsAvailable", async ctx =>
        {
            var access = await bleManager.RequestAccessAsync();
            ctx.Log($"Central access: {access}");
            Assert.Equal(AccessState.Available, access);
            Assert.Equal(AccessState.Available, bleManager.CurrentAccess);
        });

        this.Test("Central_ScanStartsAndStops", async ctx =>
        {
            await RequireCentral(ctx);
            if (bleManager.IsScanning)
                bleManager.StopScan();

            var seen = 0;
            using (bleManager.Scan().Subscribe(_ => Interlocked.Increment(ref seen)))
            {
                // Apple parks a scan until the central powers on - IsScanning reports the native scan
                await ctx.WaitUntilAsync(() => bleManager.IsScanning, TimeSpan.FromSeconds(10), "the native scan to start");
                await Task.Delay(TimeSpan.FromSeconds(3), ctx.CancellationToken);
            }
            ctx.Log($"{seen} advertisement(s) during the scan");

            await ctx.WaitUntilAsync(() => !bleManager.IsScanning, TimeSpan.FromSeconds(5), "the scan to stop after disposal");
        });

        this.Test("Central_SecondScanThrowsWhileFirstRuns", async ctx =>
        {
            await RequireCentral(ctx);
            if (bleManager.IsScanning)
                bleManager.StopScan();

            using (bleManager.Scan().Subscribe(_ => { }))
            {
                var error = await bleManager
                    .Scan()
                    .Take(1)
                    .Timeout(TimeSpan.FromSeconds(5))
                    .Select(_ => (Exception?)null)
                    .Catch<Exception?, Exception>(ex => Observable.Return<Exception?>(ex))
                    .ToTask(ctx.CancellationToken);

                ctx.Log($"Second scan: {error?.GetType().Name}: {error?.Message}");
                Assert.NotNull(error);
                Assert.IsNotType<TimeoutException>(error);
            }
        });

        this.Test("Hosting_AccessIsAvailable", async ctx =>
        {
            var access = await hosting.RequestAccess();
            ctx.Log($"Hosting access: {access} (advertise {hosting.AdvertisingAccessStatus}, gatt {hosting.GattAccessStatus})");
            Assert.Equal(AccessState.Available, access);
        });

        this.Test("Hosting_ServiceAddAndRemove", async ctx =>
        {
            await RequireHosting(ctx);
            var uuid = BleTestProfile.NewServiceUuid();
            try
            {
                var service = await hosting.AddService(uuid, true, sb =>
                    sb.AddCharacteristic(BleTestProfile.ReadUuid, cb => cb.SetRead(_ => Task.FromResult(GattResult.Success(BleTestProfile.ReadValue))))
                );
                Assert.Equal(uuid, service.Uuid, ignoreCase: true);
                Assert.Single(service.Characteristics);
                Assert.Contains(hosting.Services, x => x.Uuid.Equals(uuid, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                hosting.RemoveService(uuid);
            }
            Assert.DoesNotContain(hosting.Services, x => x.Uuid.Equals(uuid, StringComparison.OrdinalIgnoreCase));
        });

        this.Test("Hosting_AdvertisingStartsAndStops", async ctx =>
        {
            await RequireHosting(ctx);
            if (hosting.IsAdvertising)
                hosting.StopAdvertising();

            try
            {
                await hosting.StartAdvertising(new AdvertisementOptions(BleTestProfile.LocalName, BleTestProfile.NewServiceUuid()));
                await ctx.WaitUntilAsync(() => hosting.IsAdvertising, TimeSpan.FromSeconds(10), "advertising to start");
            }
            finally
            {
                hosting.StopAdvertising();
            }
            await ctx.WaitUntilAsync(() => !hosting.IsAdvertising, TimeSpan.FromSeconds(10), "advertising to stop");
        });

        this.Test("Hosting_AppleRejectsManufacturerData", async ctx =>
        {
            ctx.SkipUnless(ctx.IsApple, "CoreBluetooth-only limit - Android advertises manufacturer data");
            await RequireHosting(ctx);

            var options = new AdvertisementOptions(BleTestProfile.LocalName)
            {
                ManufacturerData = new ManufacturerData(0xFFFF, [0x01, 0x02])
            };
            await Assert.ThrowsAsync<NotSupportedException>(() => hosting.StartAdvertising(options));
            Assert.False(hosting.IsAdvertising);
        });

        this.Test("Hosting_L2CapPublishesPsm", async ctx =>
        {
            await RequireHosting(ctx);
            ctx.SkipUnless(!ctx.IsAndroid || OperatingSystem.IsAndroidVersionAtLeast(29), "L2CAP hosting needs Android API 29+");

            var instance = await hosting.OpenL2Cap(false, channel => channel.Dispose());
            try
            {
                ctx.Log($"Published PSM {instance.Psm}");
                // dynamic LE PSMs start at 0x80
                Assert.InRange(instance.Psm, (ushort)0x80, ushort.MaxValue);
            }
            finally
            {
                instance.Dispose();
            }
        });
    }


    async Task RequireCentral(TestContext ctx)
    {
        var access = await bleManager.RequestAccessAsync();
        ctx.SkipUnless(access == AccessState.Available, $"BLE central not available: {access}");
    }


    async Task RequireHosting(TestContext ctx)
    {
        var access = await hosting.RequestAccess();
        ctx.SkipUnless(access == AccessState.Available, $"BLE hosting not available: {access}");
    }
}
