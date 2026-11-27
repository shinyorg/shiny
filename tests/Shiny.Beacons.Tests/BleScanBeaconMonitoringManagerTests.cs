using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Shiny.Beacons.Tests.Fakes;
using Shiny.BluetoothLE;

namespace Shiny.Beacons.Tests;


public class BleScanBeaconMonitoringManagerTests
{
    static readonly Guid Uuid = new("E2C56DB5-DFFB-48D2-B060-D0F5A71096E0");

    readonly FakeTimeProvider clock = new(DateTimeOffset.Parse("2026-11-26T12:00:00Z"));
    readonly FakeBleManager ble = new();
    readonly InMemoryRepository repository = new();
    readonly BeaconRangingOptions options;

    public BleScanBeaconMonitoringManagerTests()
    {
        this.options = new BeaconRangingOptions
        {
            TimeProvider = this.clock,
            RegionExitTimeout = TimeSpan.FromSeconds(30)
        };
    }


    BleScanBeaconMonitoringManager Create() => new(
        this.ble,
        this.repository,
        new ServiceCollection().BuildServiceProvider(),
        this.options,
        NullLogger<IBeaconMonitoringManager>.Instance
    );


    [Fact]
    public async Task Start_RestartsTheScanForPersistedRegions()
    {
        // regions outlive the process - the pre-fix manager loaded them but never scanned for them again
        this.repository.Set(new BeaconRegion("test", Uuid));
        using var manager = this.Create();

        manager.Start();

        await WaitFor(() => this.ble.Subscriptions == 1);
    }


    [Fact]
    public async Task Start_WithNoRegions_DoesNotScan()
    {
        using var manager = this.Create();

        manager.Start();
        await Task.Delay(50);

        Assert.Equal(0, this.ble.Subscriptions);
    }


    [Fact]
    public async Task ScanFailure_IsRetriedAfterTheExitTimeout()
    {
        using var manager = this.Create();
        await manager.StartMonitoring(new BeaconRegion("test", Uuid));
        Assert.Equal(1, this.ble.Subscriptions);

        // a failed scan used to stay in place - StartScan saw it and never started another
        this.ble.Fail(new InvalidOperationException("Bluetooth off"));
        await WaitFor(() => this.ble.ActiveSubscriptions == 0);

        this.clock.Advance(TimeSpan.FromSeconds(29));
        await Task.Delay(50);
        Assert.Equal(1, this.ble.Subscriptions);

        this.clock.Advance(TimeSpan.FromSeconds(1));
        await WaitFor(() => this.ble.Subscriptions == 2);
    }


    static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(20);

        Assert.True(condition());
    }


    sealed class FakeBleManager : IBleManager
    {
        Subject<ScanResult> scan = new();
        int subscriptions;
        int active;

        public int Subscriptions => this.subscriptions;
        public int ActiveSubscriptions => this.active;

        public void Fail(Exception ex) => this.scan.OnError(ex);

        public AccessState CurrentAccess => AccessState.Available;
        public IObservable<AccessState> RequestAccess() => Observable.Return(AccessState.Available);
        public IPeripheral? GetKnownPeripheral(string peripheralUuid) => null;
        public bool IsScanning => this.active > 0;
        public void StopScan() { }
        public IEnumerable<IPeripheral> GetConnectedPeripherals() => [];

        public IObservable<ScanResult> Scan(ScanConfig? scanConfig = null) => Observable.Create<ScanResult>(ob =>
        {
            Interlocked.Increment(ref this.subscriptions);
            Interlocked.Increment(ref this.active);

            // a fresh subject per scan, as a restarted platform scan would be
            var subject = new Subject<ScanResult>();
            this.scan = subject;
            var sub = subject.Subscribe(ob);

            return () =>
            {
                sub.Dispose();
                Interlocked.Decrement(ref this.active);
            };
        });
    }
}
