using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Shiny.Locations.Tests.Fakes;
using Xunit;

namespace Shiny.Locations.Tests;


public class GeofenceDwellTrackerTests
{
    static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);
    static readonly TimeSpan Dwell = TimeSpan.FromMinutes(5);

    readonly InMemoryRepository repository = new();
    readonly FakeTimeProvider time = new(DateTimeOffset.Parse("2026-09-26T12:00:00Z"));
    readonly TaskCompletionSource<GeofenceRegion> dwelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    GeofenceState currentState = GeofenceState.Entered;
    int dwellCount;


    GeofenceDwellTracker Create() => new(
        this.repository,
        NullLogger.Instance,
        (_, _) => Task.FromResult(this.currentState),
        r =>
        {
            Interlocked.Increment(ref this.dwellCount);
            this.dwelled.TrySetResult(r);
            return Task.CompletedTask;
        },
        this.time
    );


    GeofenceRegion Region(string id = "home", TimeSpan? dwell = null)
    {
        var region = new GeofenceRegion(id, new Position(43.6, -79.4), Distance.FromMeters(200)) { DwellTime = dwell ?? Dwell };
        this.repository.Set(region);
        return region;
    }


    async Task AssertNoDwell()
    {
        // give any (incorrectly) scheduled continuation a chance to run
        await Task.Delay(100);
        Assert.Equal(0, this.dwellCount);
    }


    [Fact]
    public async Task Fires_WhenStillInsideAfterDwellTime()
    {
        var tracker = this.Create();
        var region = this.Region();

        tracker.Entered(region);
        this.time.Advance(Dwell);

        var fired = await this.dwelled.Task.WaitAsync(Wait);
        Assert.Equal("home", fired.Identifier);
        Assert.Equal(1, this.dwellCount);
        Assert.False(tracker.HasPending);

        // the stay is kept (marked fired) until the exit, so the exit does not report it again
        Assert.True(this.repository.Get<GeofenceDwellEntry>("home")!.Fired);
        await tracker.Exited(region);
        Assert.Equal(1, this.dwellCount);
        Assert.Empty(this.repository.GetAll<GeofenceDwellEntry>());
    }


    [Fact]
    public async Task DoesNotFire_BeforeDwellTime()
    {
        var tracker = this.Create();
        tracker.Entered(this.Region());

        this.time.Advance(Dwell - TimeSpan.FromSeconds(1));
        await this.AssertNoDwell();
        Assert.True(tracker.HasPending);
    }


    [Fact]
    public async Task ExitBeforeDwell_Cancels()
    {
        var tracker = this.Create();
        var region = this.Region();

        tracker.Entered(region);
        this.time.Advance(TimeSpan.FromMinutes(2));
        await tracker.Exited(region);
        this.time.Advance(Dwell);

        await this.AssertNoDwell();
        Assert.Empty(this.repository.GetAll<GeofenceDwellEntry>());
        Assert.False(tracker.HasPending);
    }


    [Fact]
    public async Task TimerDoesNotFire_WhenVerifiedStateIsNotInside_ExitDecides()
    {
        var tracker = this.Create();
        var region = this.Region();
        tracker.Entered(region);
        this.currentState = GeofenceState.Exited;

        this.time.Advance(Dwell);
        await WaitFor(() => !tracker.HasPending);
        Assert.Equal(0, this.dwellCount);

        // the stay is left for the exit to judge
        Assert.NotNull(this.repository.Get<GeofenceDwellEntry>("home"));
    }


    [Fact]
    public async Task Exit_ReportsDwell_WhenStayLastedDwellTime()
    {
        // no timer runs - like an iOS app suspended between the entry and exit events
        var tracker = this.Create();
        var region = this.Region();
        var enteredAt = this.time.GetUtcNow();
        tracker.Entered(region, enteredAt);
        tracker.Remove("never-existed"); // unrelated remove is harmless

        await tracker.Exited(region, enteredAt + TimeSpan.FromMinutes(45));

        Assert.Equal(1, this.dwellCount);
        Assert.Empty(this.repository.GetAll<GeofenceDwellEntry>());
    }


    [Fact]
    public async Task Exit_DoesNotReportDwell_WhenStayTooShort()
    {
        var tracker = this.Create();
        var region = this.Region();
        var enteredAt = this.time.GetUtcNow();
        tracker.Entered(region, enteredAt);

        await tracker.Exited(region, enteredAt + Dwell - TimeSpan.FromSeconds(1));

        Assert.Equal(0, this.dwellCount);
        Assert.Empty(this.repository.GetAll<GeofenceDwellEntry>());
    }


    [Fact]
    public async Task Exit_UsesEventTimes_NotHandlingTime()
    {
        // the platform saw a 2 minute stay an hour ago - the app only got to handle it now
        this.currentState = GeofenceState.Exited;
        var tracker = this.Create();
        var region = this.Region();
        var enteredAt = this.time.GetUtcNow() - TimeSpan.FromHours(1);

        tracker.Entered(region, enteredAt);
        await WaitFor(() => !tracker.HasPending); // overdue timer re-checks, finds the device outside, leaves it to the exit

        await tracker.Exited(region, enteredAt + TimeSpan.FromMinutes(2));
        Assert.Equal(0, this.dwellCount);
    }


    [Fact]
    public async Task Exit_WithoutEntry_ReportsNothing()
    {
        var tracker = this.Create();
        await tracker.Exited(this.Region());
        Assert.Equal(0, this.dwellCount);
    }


    [Fact]
    public async Task ReplayedEnter_KeepsOriginalEntryTime()
    {
        var tracker = this.Create();
        var region = this.Region();
        var enteredAt = this.time.GetUtcNow();
        tracker.Entered(region, enteredAt);

        // e.g. CLMonitor re-reporting "inside" after a cold start
        tracker.Entered(region, enteredAt + TimeSpan.FromMinutes(4));
        Assert.Equal(enteredAt, this.repository.Get<GeofenceDwellEntry>("home")!.EnteredAt);

        await tracker.Exited(region, enteredAt + TimeSpan.FromMinutes(6));
        Assert.Equal(1, this.dwellCount);
    }


    [Fact]
    public async Task Reevaluate_FiresTimerThatCameDueWhileSuspended()
    {
        var tracker = this.Create();
        tracker.Entered(this.Region());

        // simulate a suspended process: wall clock moved, but the timer never ran
        this.repository.Set(new GeofenceDwellEntry("home", this.time.GetUtcNow() - TimeSpan.FromMinutes(10)));
        tracker.Reevaluate();

        await this.dwelled.Task.WaitAsync(Wait);
        Assert.Equal(1, this.dwellCount);
    }


    [Fact]
    public async Task Remove_ForgetsStayWithoutReporting()
    {
        var tracker = this.Create();
        var region = this.Region();
        tracker.Entered(region);

        tracker.Remove(region.Identifier);
        this.time.Advance(Dwell);
        await tracker.Exited(region);

        await this.AssertNoDwell();
        Assert.False(tracker.HasPending);
    }


    [Fact]
    public async Task DuplicateEnter_DoesNotRestartClock()
    {
        var tracker = this.Create();
        var region = this.Region();

        tracker.Entered(region);
        this.time.Advance(TimeSpan.FromMinutes(3));
        tracker.Entered(region);
        this.time.Advance(TimeSpan.FromMinutes(2));

        await this.dwelled.Task.WaitAsync(Wait);
        Assert.Equal(1, this.dwellCount);
    }


    [Fact]
    public async Task RegionWithoutDwellTime_IsIgnored()
    {
        var tracker = this.Create();
        var region = new GeofenceRegion("plain", new Position(1, 1), Distance.FromMeters(100));

        tracker.Entered(region);
        this.time.Advance(TimeSpan.FromHours(1));

        await this.AssertNoDwell();
        Assert.False(tracker.HasPending);
        Assert.Empty(this.repository.GetAll<GeofenceDwellEntry>());
    }


    [Fact]
    public async Task Restore_FiresOverdueDwell()
    {
        this.Region();
        this.repository.Set(new GeofenceDwellEntry("home", this.time.GetUtcNow() - TimeSpan.FromMinutes(10)));

        this.Create().Restore();

        var fired = await this.dwelled.Task.WaitAsync(Wait);
        Assert.Equal("home", fired.Identifier);
    }


    [Fact]
    public async Task Restore_ResumesRemainingTime()
    {
        this.Region();
        this.repository.Set(new GeofenceDwellEntry("home", this.time.GetUtcNow() - TimeSpan.FromMinutes(4)));

        var tracker = this.Create();
        tracker.Restore();
        await this.AssertNoDwell();

        this.time.Advance(TimeSpan.FromMinutes(1));
        await this.dwelled.Task.WaitAsync(Wait);
    }


    [Fact]
    public void Restore_DropsEntriesForMissingOrNonDwellRegions()
    {
        this.repository.Set(new GeofenceRegion("nodwell", new Position(1, 1), Distance.FromMeters(100)));
        this.repository.Set(new GeofenceDwellEntry("nodwell", this.time.GetUtcNow()));
        this.repository.Set(new GeofenceDwellEntry("gone", this.time.GetUtcNow()));

        var tracker = this.Create();
        tracker.Restore();

        Assert.Empty(this.repository.GetAll<GeofenceDwellEntry>());
        Assert.False(tracker.HasPending);
    }


    [Fact]
    public async Task Clear_CancelsEverything()
    {
        var tracker = this.Create();
        tracker.Entered(this.Region("a"));
        tracker.Entered(this.Region("b"));

        tracker.Clear();
        this.time.Advance(Dwell);

        await this.AssertNoDwell();
        Assert.False(tracker.HasPending);
        Assert.Empty(this.repository.GetAll<GeofenceDwellEntry>());
    }


    [Fact]
    public async Task VerifyFailure_DoesNotFire()
    {
        var tracker = new GeofenceDwellTracker(
            this.repository,
            NullLogger.Instance,
            (_, _) => Task.FromException<GeofenceState>(new TimeoutException()),
            _ =>
            {
                Interlocked.Increment(ref this.dwellCount);
                return Task.CompletedTask;
            },
            this.time
        );
        tracker.Entered(this.Region());
        this.time.Advance(Dwell);

        await WaitFor(() => !tracker.HasPending);
        Assert.Equal(0, this.dwellCount);

        // the exit can still report it
        Assert.NotNull(this.repository.Get<GeofenceDwellEntry>("home"));
    }


    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DwellTime_MustBePositive(int seconds)
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GeofenceRegion("x", new Position(1, 1), Distance.FromMeters(100)) { DwellTime = TimeSpan.FromSeconds(seconds) }
        );


    [Theory]
    [InlineData(false, false, GeofenceState.Entered, false)]
    [InlineData(true, false, GeofenceState.Entered, true)]
    [InlineData(true, false, GeofenceState.Exited, true)]
    [InlineData(true, true, GeofenceState.Entered, false)]
    [InlineData(true, true, GeofenceState.Exited, false)]
    [InlineData(true, true, GeofenceState.Dwelling, true)]
    public void IsSingleUseComplete(bool singleUse, bool hasDwell, GeofenceState state, bool expected)
    {
        var region = new GeofenceRegion("x", new Position(1, 1), Distance.FromMeters(100), singleUse)
        {
            DwellTime = hasDwell ? Dwell : null
        };
        Assert.Equal(expected, region.IsSingleUseComplete(state));
    }


    [Fact]
    public void DwellTime_RoundTripsThroughJsonContext()
    {
        var region = new GeofenceRegion("x", new Position(1, 2), Distance.FromMeters(100), true, false, true) { DwellTime = Dwell };
        var json = JsonSerializer.Serialize(region, ShinyLocationsJsonContext.Default.GeofenceRegion);
        var back = JsonSerializer.Deserialize(json, ShinyLocationsJsonContext.Default.GeofenceRegion);

        Assert.Equal(region, back);
        Assert.Equal(Dwell, back!.DwellTime);

        var legacy = JsonSerializer.Deserialize("""{"Identifier":"old","Center":{"Latitude":1,"Longitude":2},"Radius":{"TotalMeters":100}}""", ShinyLocationsJsonContext.Default.GeofenceRegion);
        Assert.Null(legacy!.DwellTime);
    }


    static async Task WaitFor(Func<bool> condition)
    {
        var until = DateTime.UtcNow + Wait;
        while (!condition() && DateTime.UtcNow < until)
            await Task.Delay(10);

        Assert.True(condition());
    }
}
