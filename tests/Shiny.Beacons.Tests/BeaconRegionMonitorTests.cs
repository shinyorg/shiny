using Microsoft.Extensions.Time.Testing;
using Shiny.Beacons.Infrastructure;

namespace Shiny.Beacons.Tests;


public class BeaconRegionMonitorTests
{
    static readonly Guid Uuid = new("E2C56DB5-DFFB-48D2-B060-D0F5A71096E0");

    readonly FakeTimeProvider clock = new(DateTimeOffset.Parse("2026-09-09T12:00:00Z"));
    readonly BeaconRangingOptions options;
    readonly BeaconRegionMonitor monitor;

    public BeaconRegionMonitorTests()
    {
        this.options = new BeaconRangingOptions
        {
            TimeProvider = this.clock,
            RegionExitTimeout = TimeSpan.FromSeconds(30)
        };
        this.monitor = new BeaconRegionMonitor(this.options);
    }


    [Fact]
    public void FirstSighting_RaisesEntered()
    {
        // The pre-revival monitor seeded the first sighting as "already inside" and then tested for
        // a change, so a region entered from cold never raised an event at all. This is that bug.
        this.monitor.AddRegion(new BeaconRegion("test", Uuid));

        var transitions = this.monitor.Report(new BeaconIdentity(Uuid, 1, 1));

        var transition = Assert.Single(transitions);
        Assert.Equal(BeaconRegionState.Entered, transition.State);
        Assert.Equal("test", transition.Region.Identifier);
        Assert.Equal(BeaconRegionState.Entered, this.monitor.GetState("test"));
    }


    [Fact]
    public void RepeatSightings_RaiseNothingFurther()
    {
        this.monitor.AddRegion(new BeaconRegion("test", Uuid));
        this.monitor.Report(new BeaconIdentity(Uuid, 1, 1));

        Assert.Empty(this.monitor.Report(new BeaconIdentity(Uuid, 1, 1)));
        Assert.Empty(this.monitor.Report(new BeaconIdentity(Uuid, 1, 1)));
    }


    [Fact]
    public void Silence_RaisesExitedOnceTheTimeoutElapses()
    {
        this.monitor.AddRegion(new BeaconRegion("test", Uuid));
        this.monitor.Report(new BeaconIdentity(Uuid, 1, 1));

        this.clock.Advance(TimeSpan.FromSeconds(29));
        Assert.Empty(this.monitor.Evaluate());

        this.clock.Advance(TimeSpan.FromSeconds(2));
        var transition = Assert.Single(this.monitor.Evaluate());
        Assert.Equal(BeaconRegionState.Exited, transition.State);
    }


    [Fact]
    public void Exit_RaisesOnlyOnce()
    {
        this.monitor.AddRegion(new BeaconRegion("test", Uuid));
        this.monitor.Report(new BeaconIdentity(Uuid, 1, 1));

        this.clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Single(this.monitor.Evaluate());
        Assert.Empty(this.monitor.Evaluate());
    }


    [Fact]
    public void ContinuedSightings_KeepTheRegionAlive()
    {
        this.monitor.AddRegion(new BeaconRegion("test", Uuid));
        this.monitor.Report(new BeaconIdentity(Uuid, 1, 1));

        for (var i = 0; i < 10; i++)
        {
            this.clock.Advance(TimeSpan.FromSeconds(20));
            this.monitor.Report(new BeaconIdentity(Uuid, 1, 1));
            Assert.Empty(this.monitor.Evaluate());
        }
    }


    [Fact]
    public void ReEntry_AfterExit_RaisesEnteredAgain()
    {
        this.monitor.AddRegion(new BeaconRegion("test", Uuid));
        this.monitor.Report(new BeaconIdentity(Uuid, 1, 1));

        this.clock.Advance(TimeSpan.FromMinutes(1));
        this.monitor.Evaluate();

        var transition = Assert.Single(this.monitor.Report(new BeaconIdentity(Uuid, 1, 1)));
        Assert.Equal(BeaconRegionState.Entered, transition.State);
    }


    [Fact]
    public void AddRegion_Twice_DoesNotThrow()
    {
        // the pre-revival monitor used Dictionary.Add and crashed with a duplicate key when a
        // region was re-registered or the scan restarted
        var region = new BeaconRegion("test", Uuid);

        this.monitor.AddRegion(region);
        this.monitor.AddRegion(region);

        Assert.Equal(1, this.monitor.Count);
    }


    [Fact]
    public void SetRegions_PreservesStateOfRegionsThatStay()
    {
        var stays = new BeaconRegion("stays", Uuid);
        var goes = new BeaconRegion("goes", Uuid);

        this.monitor.SetRegions([stays, goes]);
        this.monitor.Report(new BeaconIdentity(Uuid, 1, 1));

        this.monitor.SetRegions([stays]);

        Assert.Equal(1, this.monitor.Count);
        Assert.Equal(BeaconRegionState.Entered, this.monitor.GetState("stays"));
        Assert.Equal(BeaconRegionState.Unknown, this.monitor.GetState("goes"));

        // and it does not re-fire an entry the delegate already heard about
        Assert.Empty(this.monitor.Report(new BeaconIdentity(Uuid, 1, 1)));
    }


    [Fact]
    public void NotifyFlags_SuppressTransitions()
    {
        this.monitor.AddRegion(new BeaconRegion("entryonly", Uuid, NotifyOnExit: false));
        this.monitor.AddRegion(new BeaconRegion("exitonly", Uuid, NotifyOnEntry: false));

        var entered = Assert.Single(this.monitor.Report(new BeaconIdentity(Uuid, 1, 1)));
        Assert.Equal("entryonly", entered.Region.Identifier);

        this.clock.Advance(TimeSpan.FromMinutes(1));
        var exited = Assert.Single(this.monitor.Evaluate());
        Assert.Equal("exitonly", exited.Region.Identifier);
    }


    [Fact]
    public void BeaconOutsideTheRegion_IsIgnored()
    {
        this.monitor.AddRegion(new BeaconRegion("test", Uuid, Major: 5));

        Assert.Empty(this.monitor.Report(new BeaconIdentity(Uuid, 4, 1)));
        Assert.Empty(this.monitor.Report(new BeaconIdentity(Guid.NewGuid(), 5, 1)));
        Assert.Single(this.monitor.Report(new BeaconIdentity(Uuid, 5, 1)));
    }


    [Fact]
    public void OneBeacon_EntersEveryMatchingRegion()
    {
        this.monitor.AddRegion(new BeaconRegion("broad", Uuid));
        this.monitor.AddRegion(new BeaconRegion("narrow", Uuid, Major: 1, Minor: 2));

        var transitions = this.monitor.Report(new BeaconIdentity(Uuid, 1, 2));
        Assert.Equal(2, transitions.Count);
    }
}
