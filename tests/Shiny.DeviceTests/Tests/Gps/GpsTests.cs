using Shiny.Locations;

namespace Shiny.DeviceTests.Tests;


public class TestGpsDelegate(EventHub events) : IGpsDelegate
{
    public const string Topic = "gps.delegate";

    public Task OnReading(GpsReading reading)
    {
        events.Publish(Topic, reading);
        return Task.CompletedTask;
    }
}


public class GpsTests(IGpsManager gps) : DeviceTestSuite
{
    static readonly TimeSpan ReadingTimeout = TimeSpan.FromSeconds(45);
    AccessState access = AccessState.Unknown;

    public override string Name => "Gps";
    protected override TimeSpan DefaultTimeout => TimeSpan.FromMinutes(2);


    public override async Task SetupAsync(TestContext context)
    {
        // a listener left behind by a previous (crashed) run is restarted at startup - start from nothing
        await gps.StopListener();
        this.access = await gps.RequestAccess(GpsRequest.Foreground);
        context.Log($"Foreground access: {this.access}");
    }


    public override Task TeardownAsync(TestContext context) => gps.StopListener();


    protected override void Define()
    {
        this.Test("RequestAccess_Foreground_IsAvailable", ctx =>
        {
            // permissions are only pre-granted (simctl privacy / pm grant) where the runner controls the device
            ctx.RequireCapability(HostCapability.Location);
            Assert.Equal(AccessState.Available, this.access);
            Assert.Equal(AccessState.Available, gps.GetCurrentStatus(GpsRequest.Foreground));
            return Task.CompletedTask;
        });

        this.Test("StopListener_WhenNotListening_IsNoOp", async _ =>
        {
            Assert.Null(gps.CurrentListener);
            await gps.StopListener();
            Assert.Null(gps.CurrentListener);
            Assert.False(gps.IsListening());
        });

        this.Test("StartListener_WhenAlreadyListening_Throws", async ctx =>
        {
            this.RequireAccess(ctx);
            await gps.StartListener(GpsRequest.Foreground);
            try
            {
                Assert.True(gps.IsListening());
                await Assert.ThrowsAsync<InvalidOperationException>(() => gps.StartListener(GpsRequest.Foreground));

                // the failed second start must not disturb the running listener
                Assert.True(gps.IsListening());
            }
            finally
            {
                await gps.StopListener();
            }
            Assert.Null(gps.CurrentListener);
        });

        this.Test("CurrentListener_PreservesRequestOptions", async ctx =>
        {
            this.RequireAccess(ctx);
            var request = new GpsRequest(GpsBackgroundMode.None, RequestPreciseAccuracy: true, AutoRestart: false);
            await gps.StartListener(request);
            try
            {
                var current = gps.CurrentListener;
                Assert.NotNull(current);
                ctx.Log($"CurrentListener: {current}");
                Assert.Equal(GpsBackgroundMode.None, current.BackgroundMode);
                Assert.True(current.RequestPreciseAccuracy, "RequestPreciseAccuracy was dropped from the active request");
                Assert.False(current.AutoRestart, "AutoRestart was dropped from the active request");
            }
            finally
            {
                await gps.StopListener();
            }
        });

        this.Test("Listener_DeliversSimulatedPositions_InOrder", async ctx =>
        {
            ctx.RequireCapability(HostCapability.Location);
            this.RequireAccess(ctx);

            var eventReadings = new List<GpsReading>();
            EventHandler<GpsReading> handler = (_, r) =>
            {
                lock (eventReadings)
                    eventReadings.Add(r);
            };

            gps.GpsReadingReceived += handler;
            await SimulatedLocation.SetAsync(ctx, SimulatedLocation.A);
            await gps.StartListener(GpsRequest.Foreground);
            try
            {
                foreach (var target in new[] { SimulatedLocation.A, SimulatedLocation.B, SimulatedLocation.C })
                {
                    var reading = await SimulatedLocation.HoldAsync(ctx, target, this.WaitForReadingNear(ctx, target));
                    ctx.Log($"delegate reading {reading.Position.Latitude}, {reading.Position.Longitude} ±{reading.PositionAccuracy}m");
                }
            }
            finally
            {
                gps.GpsReadingReceived -= handler;
                await gps.StopListener();
            }

            // the delegate saw A, then B, then C
            var delegateReadings = ctx.Events.Get<GpsReading>(TestGpsDelegate.Topic).ToList();
            var a = delegateReadings.FindIndex(x => x.IsNear(SimulatedLocation.A));
            var b = delegateReadings.FindIndex(x => x.IsNear(SimulatedLocation.B));
            var c = delegateReadings.FindIndex(x => x.IsNear(SimulatedLocation.C));
            Assert.True(a >= 0 && a < b && b < c, $"Expected delegate readings in order A<B<C, got indexes {a}, {b}, {c}");

            // the foreground event carries the same readings
            List<GpsReading> events;
            lock (eventReadings)
                events = eventReadings.ToList();

            Assert.Contains(events, x => x.IsNear(SimulatedLocation.C));

            // timestamps never go backwards
            var timestamps = delegateReadings.Select(x => x.Timestamp).ToList();
            Assert.Equal(timestamps.OrderBy(x => x), timestamps);
        });

        this.Test("StopListener_StopsReadings", async ctx =>
        {
            ctx.RequireCapability(HostCapability.Location);
            this.RequireAccess(ctx);

            await gps.StartListener(GpsRequest.Foreground);
            await SimulatedLocation.HoldAsync(ctx, SimulatedLocation.A, this.WaitForReadingNear(ctx, SimulatedLocation.A));
            await gps.StopListener();
            Assert.Null(gps.CurrentListener);

            ctx.Events.Clear(TestGpsDelegate.Topic);
            await SimulatedLocation.SetAsync(ctx, SimulatedLocation.C);
            await Task.Delay(TimeSpan.FromSeconds(8), ctx.CancellationToken);

            var late = ctx.Events.Get<GpsReading>(TestGpsDelegate.Topic);
            Assert.DoesNotContain(late, x => x.IsNear(SimulatedLocation.C));
        });

        this.Test("GetLastReading_ReturnsLastSimulatedPosition", async ctx =>
        {
            ctx.RequireCapability(HostCapability.Location);
            this.RequireAccess(ctx);

            await gps.StartListener(GpsRequest.Foreground);
            try
            {
                await SimulatedLocation.HoldAsync(ctx, SimulatedLocation.B, this.WaitForReadingNear(ctx, SimulatedLocation.B));
            }
            finally
            {
                await gps.StopListener();
            }

            var last = await gps.GetLastReading();
            Assert.NotNull(last);
            ctx.Log($"last reading {last.Position.Latitude}, {last.Position.Longitude} at {last.Timestamp}");
            Assert.True(last.IsNear(SimulatedLocation.B), $"Last reading {last.Position} is not near {SimulatedLocation.B}");
        });

        this.Test("GetCurrentPosition_ReturnsSimulatedPosition_AndLeavesNoListener", async ctx =>
        {
            ctx.RequireCapability(HostCapability.Location);
            this.RequireAccess(ctx);
            Assert.False(gps.IsListening());

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctx.CancellationToken);
            cts.CancelAfter(ReadingTimeout);

            // move first - GetCurrentPosition takes the first reading its listener gets, which is CoreLocation's cached
            // fix if the move hasn't landed yet. Then keep the fix flowing for Android's fused provider.
            await SimulatedLocation.SetAsync(ctx, SimulatedLocation.C);
            await Task.Delay(2000, ctx.CancellationToken);
            var reading = await SimulatedLocation.HoldAsync(ctx, SimulatedLocation.C, gps.GetCurrentPosition(cts.Token));
            Assert.NotNull(reading);
            ctx.Log($"current position {reading.Position.Latitude}, {reading.Position.Longitude}");
            Assert.True(reading.IsNear(SimulatedLocation.C), $"Current position {reading.Position} is not near {SimulatedLocation.C}");

            // it started the listener, so it must stop it again
            Assert.False(gps.IsListening());
        });

        this.Test("IsInsideRegion_UsesCurrentPosition", async ctx =>
        {
            ctx.RequireCapability(HostCapability.Location);
            this.RequireAccess(ctx);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctx.CancellationToken);
            cts.CancelAfter(ReadingTimeout);

            // move first, for the same cached-fix reason as GetCurrentPosition
            await SimulatedLocation.SetAsync(ctx, SimulatedLocation.A);
            await Task.Delay(2000, ctx.CancellationToken);
            var inside = await SimulatedLocation.HoldAsync(ctx, SimulatedLocation.A, gps.IsInsideRegion(SimulatedLocation.A, Distance.FromMeters(500), cts.Token));
            Assert.True(inside);

            var outside = await gps.IsInsideRegion(SimulatedLocation.C, Distance.FromMeters(500), cts.Token);
            Assert.False(outside);
        });

        this.Test("RealtimeBackgroundListener_DeliversReadings", async ctx =>
        {
            ctx.RequireCapability(HostCapability.Location);
            var request = GpsRequest.Realtime(true);
            var bgAccess = await gps.RequestAccess(request);
            ctx.Log($"Realtime access: {bgAccess}");
            Assert.Equal(AccessState.Available, bgAccess);

            await gps.StartListener(request);
            try
            {
                Assert.Equal(GpsBackgroundMode.Realtime, gps.CurrentListener?.BackgroundMode);
                await SimulatedLocation.HoldAsync(ctx, SimulatedLocation.B, this.WaitForReadingNear(ctx, SimulatedLocation.B));
            }
            finally
            {
                await gps.StopListener();
            }
            Assert.Null(gps.CurrentListener);
        });
    }


    void RequireAccess(TestContext ctx)
        => ctx.SkipUnless(this.access == AccessState.Available, $"Location access is {this.access}");


    Task<GpsReading> WaitForReadingNear(TestContext ctx, Position position) => ctx.Events.WaitAsync<GpsReading>(
        TestGpsDelegate.Topic,
        ReadingTimeout,
        ctx.CancellationToken,
        x => x.IsNear(position)
    );
}
