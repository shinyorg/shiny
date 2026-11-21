using Shiny.Beacons;
#if IOS || MACCATALYST
using CoreLocation;
#endif

namespace Shiny.DeviceTests.Tests;


public record BeaconMonitorEvent(BeaconRegionState State, BeaconRegion Region);


public class TestBeaconMonitorDelegate(EventHub events) : IBeaconMonitorDelegate
{
    public const string Topic = "beacons.monitor";

    public Task OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region)
    {
        events.Publish(Topic, new BeaconMonitorEvent(newStatus, region));
        return Task.CompletedTask;
    }
}


// No simulator or emulator can produce an iBeacon, so these cover permissions and region bookkeeping only.
public class BeaconsTests(IBeaconMonitoringManager monitoring) : DeviceTestSuite
{
    // room for the first ask to sit out the one-time upgrade prompt (30s cap) when only when-in-use is granted
    static readonly TimeSpan PromptTimeout = TimeSpan.FromSeconds(90);
    static readonly Guid Uuid = Guid.Parse("E2C56DB5-DFFB-48D2-B060-D0F5A71096E0");

    public override string Name => "Beacons";
    protected override TimeSpan DefaultTimeout => TimeSpan.FromSeconds(30);


    public override Task SetupAsync(TestContext context) => monitoring.StopAllMonitoring();
    public override Task TeardownAsync(TestContext context) => monitoring.StopAllMonitoring();


    protected override void Define()
    {
        this.Test("RequestAccess_MatchesSystemStatus", async ctx =>
        {
            await LocationAuthorization.AssertAnswersForAlways(ctx, "RequestAccess", monitoring.RequestAccess);
            Assert.Equal(LocationAuthorization.ExpectedForAlways(), monitoring.CurrentStatus);
        }, timeout: PromptTimeout);

        this.Test("RequestAccess_ConcurrentCallsAgree", async ctx =>
        {
            var expected = LocationAuthorization.ExpectedForAlways();
            var results = await Task.WhenAll(monitoring.RequestAccess(), monitoring.RequestAccess(), monitoring.RequestAccess());
            ctx.Log($"RequestAccess x3: {String.Join(", ", results)}");
            Assert.All(results, x => Assert.Equal(expected, x));
        }, timeout: PromptTimeout);

#if IOS || MACCATALYST
        this.Test("AppleLocationAccess_MatchesSystemStatus", ctx => LocationAuthorization.AssertAnswersForAlways(
            ctx,
            "AppleLocationAccess.RequestAccess",
            () => MainThread.InvokeOnMainThreadAsync(() =>
            {
                // created here so the manager's own creation callback races the request, as it does in an app
                var manager = new CLLocationManager { Delegate = new TestBeaconLocationDelegate() };
                return manager.RequestAccess(true);
            })
        ), timeout: PromptTimeout);
#endif

        this.Test("StartMonitoring_RoundTripsAndStops", async ctx =>
        {
            var region = NewRegion() with { Major = 7, NotifyOnExit = false };
            await monitoring.StartMonitoring(region);

            var stored = Assert.Single(monitoring.GetMonitoredRegions(), x => x.Identifier == region.Identifier);
            Assert.Equal(region, stored);

            var state = await monitoring.RequestState(region, ctx.CancellationToken);
            ctx.Log($"RequestState with no beacon around: {state}");
            Assert.NotEqual(BeaconRegionState.Entered, state);

            await monitoring.StopMonitoring(region.Identifier);
            Assert.DoesNotContain(monitoring.GetMonitoredRegions(), x => x.Identifier == region.Identifier);
        });

        this.Test("StartMonitoring_SameIdentifierReplaces", async ctx =>
        {
            var region = NewRegion() with { Major = 1 };
            await monitoring.StartMonitoring(region);
            await monitoring.StartMonitoring(region with { Major = 2, Minor = 3 });

            var stored = Assert.Single(monitoring.GetMonitoredRegions(), x => x.Identifier == region.Identifier);
            Assert.Equal((ushort?)2, stored.Major);
            Assert.Equal((ushort?)3, stored.Minor);
        });

        this.Test("StartMonitoring_NothingToNotify_ThrowsAndStoresNothing", async ctx =>
        {
            var region = NewRegion() with { NotifyOnEntry = false, NotifyOnExit = false };
            await Assert.ThrowsAsync<InvalidOperationException>(() => monitoring.StartMonitoring(region));
            Assert.DoesNotContain(monitoring.GetMonitoredRegions(), x => x.Identifier == region.Identifier);
        });

        this.Test("StartMonitoring_RejectedReplacement_KeepsTheOriginal", async ctx =>
        {
            var region = NewRegion() with { Major = 5 };
            await monitoring.StartMonitoring(region);
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => monitoring.StartMonitoring(region with { NotifyOnEntry = false, NotifyOnExit = false })
            );

            var stored = Assert.Single(monitoring.GetMonitoredRegions(), x => x.Identifier == region.Identifier);
            Assert.Equal(region, stored);
        });
    }


    static BeaconRegion NewRegion() => new($"devicetests-{Guid.NewGuid():N}", Uuid);
}


#if IOS || MACCATALYST
class TestBeaconLocationDelegate : ShinyBeaconLocationDelegate;
#endif
