using Shiny.Locations;

namespace Shiny.DeviceTests.Tests;


public record GeofenceEvent(GeofenceState State, GeofenceRegion Region);


public class TestGeofenceDelegate(EventHub events) : IGeofenceDelegate
{
    public const string Topic = "geofence.delegate";

    public Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
    {
        events.Publish(Topic, new GeofenceEvent(newStatus, region));
        return Task.CompletedTask;
    }
}


public class GeofencingTests(IGeofenceManager geofences, IGpsManager gps) : DeviceTestSuite
{
    // OS geofence delivery on a simulator/emulator is measured in tens of seconds, sometimes minutes
    static readonly TimeSpan TransitionTimeout = TimeSpan.FromMinutes(3);
    static readonly TimeSpan StateTimeout = TimeSpan.FromSeconds(90);
    static readonly Distance Radius = Distance.FromMeters(200);

    // the fence sits on B; A and C are ~1.1km either side of it
    static Position Inside => SimulatedLocation.B;
    static Position Outside => SimulatedLocation.A;

    AccessState access = AccessState.Unknown;

    public override string Name => "Geofencing";
    protected override TimeSpan DefaultTimeout => TimeSpan.FromMinutes(2);


    public override async Task SetupAsync(TestContext context)
    {
#if ANDROID
        var playServices = Android.Gms.Common.GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Android.App.Application.Context);
        if (playServices != Android.Gms.Common.ConnectionResult.Success)
            TestContext.Skip($"Android geofencing needs Google Play Services (status {playServices}) - use a Google APIs emulator image");
#endif
        this.access = await geofences.RequestAccess();
        context.Log($"Geofence access: {this.access}");
        await geofences.StopAllMonitoring();
    }


    public override async Task TeardownAsync(TestContext context)
    {
        await geofences.StopAllMonitoring();
        await gps.StopListener();
    }


    protected override void Define()
    {
        this.Test("RequestAccess_IsAvailable", ctx =>
        {
            ctx.RequireCapability(HostCapability.Location);
            Assert.Equal(AccessState.Available, this.access);
            Assert.Equal(AccessState.Available, geofences.CurrentStatus);
            return Task.CompletedTask;
        });

        this.Test("StartMonitoring_RoundTripsThroughGetMonitorRegions", async ctx =>
        {
            this.RequireAccess(ctx);
            var region = NewRegion() with { NotifyOnEntry = false };
            await geofences.StartMonitoring(region);

            var stored = Assert.Single(geofences.GetMonitorRegions(), x => x.Identifier == region.Identifier);
            Assert.Equal(region.Center, stored.Center);
            Assert.Equal(region.Radius.TotalMeters, stored.Radius.TotalMeters, 3);
            Assert.False(stored.NotifyOnEntry);
            Assert.True(stored.NotifyOnExit);
            Assert.False(stored.SingleUse);

            await geofences.StopMonitoring(region.Identifier);
            Assert.DoesNotContain(geofences.GetMonitorRegions(), x => x.Identifier == region.Identifier);
        });

        this.Test("StartMonitoring_SameIdentifier_ReplacesRegion", async ctx =>
        {
            this.RequireAccess(ctx);
            var region = NewRegion();
            await geofences.StartMonitoring(region);
            await geofences.StartMonitoring(region with { Radius = Distance.FromMeters(500) });

            var stored = Assert.Single(geofences.GetMonitorRegions(), x => x.Identifier == region.Identifier);
            Assert.Equal(500, stored.Radius.TotalMeters, 3);
            await geofences.StopMonitoring(region.Identifier);
        });

        this.Test("StopAllMonitoring_ClearsEveryRegion", async ctx =>
        {
            this.RequireAccess(ctx);
            await geofences.StartMonitoring(NewRegion());
            await geofences.StartMonitoring(NewRegion(SimulatedLocation.C));
            Assert.Equal(2, geofences.GetMonitorRegions().Count);

            await geofences.StopAllMonitoring();
            Assert.Empty(geofences.GetMonitorRegions());
        });

        this.Test("StartMonitoring_WhileInside_ReportsStateWithoutEntered", async ctx =>
        {
            this.RequireLocation(ctx);
            await this.DriveGpsAsync(ctx, async () =>
            {
                await SimulatedLocation.SetAsync(ctx, Inside);
                var region = NewRegion();
                await geofences.StartMonitoring(region);

                await SimulatedLocation.HoldAsync(ctx, Inside, this.WaitForStateAsync(ctx, region, GeofenceState.Entered));

                // already being inside when monitoring starts never reports Entered (documented contract on both OSes)
                await Task.Delay(TimeSpan.FromSeconds(10), ctx.CancellationToken);
                Assert.DoesNotContain(Events(ctx, region), x => x.State == GeofenceState.Entered);
            });
        }, TimeSpan.FromMinutes(3));

        this.Test("EnterThenExit_FiresDelegate", async ctx =>
        {
            this.RequireLocation(ctx);
            await this.DriveGpsAsync(ctx, async () =>
            {
                var region = await this.StartOutsideAsync(ctx, NewRegion());

                var entered = await SimulatedLocation.HoldAsync(ctx, Inside, this.WaitForEventAsync(ctx, region, GeofenceState.Entered));
                Assert.Equal(region.Identifier, entered.Region.Identifier);
                Assert.Equal(GeofenceState.Entered, await geofences.RequestState(region, ctx.CancellationToken));

                var exited = await SimulatedLocation.HoldAsync(ctx, Outside, this.WaitForEventAsync(ctx, region, GeofenceState.Exited));
                Assert.Equal(region.Identifier, exited.Region.Identifier);
                Assert.Equal(GeofenceState.Exited, await geofences.RequestState(region, ctx.CancellationToken));

                // one entry, one exit - no duplicates for a single crossing each way
                var all = Events(ctx, region);
                Assert.Single(all, x => x.State == GeofenceState.Entered);
                Assert.Single(all, x => x.State == GeofenceState.Exited);
            });
        }, TimeSpan.FromMinutes(8));

        this.Test("NotifyOnEntryFalse_OnlyReportsExit", async ctx =>
        {
            this.RequireLocation(ctx);
            await this.DriveGpsAsync(ctx, async () =>
            {
                var region = await this.StartOutsideAsync(ctx, NewRegion() with { NotifyOnEntry = false });

                // entry is still tracked (RequestState), just not delivered
                await SimulatedLocation.HoldAsync(ctx, Inside, this.WaitForStateAsync(ctx, region, GeofenceState.Entered));
                await SimulatedLocation.HoldAsync(ctx, Outside, this.WaitForEventAsync(ctx, region, GeofenceState.Exited));

                Assert.DoesNotContain(Events(ctx, region), x => x.State == GeofenceState.Entered);
            });
        }, TimeSpan.FromMinutes(8));

        this.Test("SingleUse_IsRemovedAfterFirstTransition", async ctx =>
        {
            this.RequireLocation(ctx);
            await this.DriveGpsAsync(ctx, async () =>
            {
                var region = await this.StartOutsideAsync(ctx, NewRegion() with { SingleUse = true });
                await SimulatedLocation.HoldAsync(ctx, Inside, this.WaitForEventAsync(ctx, region, GeofenceState.Entered));

                await ctx.WaitUntilAsync(
                    () => geofences.GetMonitorRegions().All(x => x.Identifier != region.Identifier),
                    TimeSpan.FromSeconds(15),
                    "the single-use region to be removed"
                );
            });
        }, TimeSpan.FromMinutes(6));
    }


    // the region is registered while outside and the OS has settled on Exited, so the next move inside is a real crossing
    async Task<GeofenceRegion> StartOutsideAsync(TestContext ctx, GeofenceRegion region)
    {
        await SimulatedLocation.SetAsync(ctx, Outside);
        await geofences.StartMonitoring(region);
        await SimulatedLocation.HoldAsync(ctx, Outside, this.WaitForStateAsync(ctx, region, GeofenceState.Exited));
        ctx.Log($"{region.Identifier} settled outside");
        return region;
    }


    // the Android fused provider only takes new emulator fixes while someone is requesting locations - keep a
    // foreground GPS listener running for the duration (harmless on iOS)
    async Task DriveGpsAsync(TestContext ctx, Func<Task> body)
    {
        var started = false;
        if (ctx.IsAndroid && !gps.IsListening())
        {
            await gps.StartListener(GpsRequest.Foreground);
            started = true;
        }
        try
        {
            await body();
        }
        finally
        {
            if (started)
                await gps.StopListener();
        }
    }


    async Task<bool> WaitForStateAsync(TestContext ctx, GeofenceRegion region, GeofenceState expected)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctx.CancellationToken);
        cts.CancelAfter(StateTimeout);

        var state = GeofenceState.Unknown;
        try
        {
            while (state != expected)
            {
                state = await geofences.RequestState(region, cts.Token);
                if (state != expected)
                    await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
            }
        }
        catch (OperationCanceledException) when (!ctx.CancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"{region.Identifier} never reported {expected} (last: {state}) within {StateTimeout.TotalSeconds:0}s");
        }
        ctx.Log($"{region.Identifier} state {state}");
        return true;
    }


    Task<GeofenceEvent> WaitForEventAsync(TestContext ctx, GeofenceRegion region, GeofenceState state) => ctx.Events.WaitAsync<GeofenceEvent>(
        TestGeofenceDelegate.Topic,
        TransitionTimeout,
        ctx.CancellationToken,
        x => x.State == state && x.Region.Identifier == region.Identifier
    );


    static List<GeofenceEvent> Events(TestContext ctx, GeofenceRegion region) => ctx.Events
        .Get<GeofenceEvent>(TestGeofenceDelegate.Topic)
        .Where(x => x.Region.Identifier == region.Identifier)
        .ToList();


    static GeofenceRegion NewRegion(Position? center = null)
        => new($"devicetests-{Guid.NewGuid():N}", center ?? Inside, Radius);


    void RequireAccess(TestContext ctx)
        => ctx.SkipUnless(this.access == AccessState.Available, $"Geofence access is {this.access}");


    void RequireLocation(TestContext ctx)
    {
        ctx.RequireCapability(HostCapability.Location);
        this.RequireAccess(ctx);
    }
}
