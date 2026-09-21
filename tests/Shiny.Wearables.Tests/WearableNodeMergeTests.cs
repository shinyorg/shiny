using Shiny.Wearables.Infrastructure;
using Xunit;

namespace Shiny.Wearables.Tests;


/// <summary>
/// The Wear OS node merge — how "connected right now" and "has the companion app" are combined into one status.
/// </summary>
/// <remarks>
/// <para>This is the part that was wrong, and the failure was invisible on a desk: the node list was built from the
/// connected nodes alone, so everything passed with a watch on the wrist and the status collapsed the moment the watch
/// went out of range. No emulator run would have caught it, because an emulated watch is never away.</para>
/// <para>The distinction these tests pin is the one shinypickle's <c>IWatchBridge</c> draws with two separate flags:
/// <c>IsSupported</c>, a persistent "this user has a watch set up", and <c>IsReachable</c>, a live "the watch is here".
/// Collapsing the two makes a "send to watch" feature appear and disappear as the user walks around the house.</para>
/// </remarks>
public class WearableNodeMergeTests
{
    static RawWearableNode Node(string id, string name = "Pixel Watch", bool nearby = true)
        => new(id, name, nearby);


    [Fact]
    public void APairedWatchThatIsAwayIsStillPairedAndStillHasTheApp()
    {
        // the regression case: the watch advertises the capability but is not connected, so GetConnectedNodes is empty
        var status = WearableNodeMerge.Build([], [Node("watch-1", nearby: false)]);

        Assert.True(status.IsPaired);
        Assert.True(status.IsAppInstalled);
        Assert.True(status.IsConfigured);

        // ... but nothing can be sent to it right now
        Assert.False(status.IsReachable);

        var node = Assert.Single(status.Nodes);
        Assert.False(node.IsConnected);
        Assert.False(node.IsNearby);
        Assert.True(node.HasApp);
    }


    [Fact]
    public void NoWatchAtAllIsDistinguishableFromAWatchThatIsAway()
    {
        var away = WearableNodeMerge.Build([], [Node("watch-1", nearby: false)]);
        var none = WearableNodeMerge.Build([], []);

        // both are unreachable; only one means "this user has no watch", and the whole fix is that they now differ
        Assert.False(away.IsReachable);
        Assert.False(none.IsReachable);

        Assert.True(away.IsConfigured);
        Assert.False(none.IsConfigured);
        Assert.Empty(none.Nodes);
    }


    [Fact]
    public void AConnectedWatchIsListedOnceNotTwice()
    {
        // the same node comes back from both sources - it is connected AND it advertises the capability
        var status = WearableNodeMerge.Build([Node("watch-1")], [Node("watch-1", nearby: false)]);

        var node = Assert.Single(status.Nodes);
        Assert.True(node.IsConnected);
        Assert.True(node.IsNearby);
        Assert.True(node.HasApp);
        Assert.True(status.IsReachable);
    }


    [Fact]
    public void TheConnectedEntryWinsOverTheCapabilityEntry()
    {
        // the capability list's copy has no liveness and can carry a stale name; the connected one is authoritative
        var status = WearableNodeMerge.Build(
            [Node("watch-1", "Allan's Watch")],
            [Node("watch-1", "Pixel Watch", nearby: false)]
        );

        var node = Assert.Single(status.Nodes);
        Assert.Equal("Allan's Watch", node.DisplayName);
        Assert.True(node.IsNearby);
    }


    [Fact]
    public void AConnectedWatchWithoutTheCompanionAppIsPairedButNotInstalled()
    {
        var status = WearableNodeMerge.Build([Node("watch-1")], []);

        Assert.True(status.IsPaired);
        Assert.False(status.IsAppInstalled);
        Assert.False(status.IsConfigured);
        Assert.False(status.IsReachable);
        Assert.False(Assert.Single(status.Nodes).HasApp);
    }


    [Fact]
    public void ACloudConnectedNodeIsConnectedButNotReachable()
    {
        // Wear OS reaches a node over the cloud when Bluetooth is out of range. Transfers still queue through it, but
        // a live message would take seconds, so it does not count as reachable.
        var status = WearableNodeMerge.Build([Node("watch-1", nearby: false)], [Node("watch-1", nearby: false)]);

        var node = Assert.Single(status.Nodes);
        Assert.True(node.IsConnected);
        Assert.False(node.IsNearby);

        Assert.True(status.IsConfigured);
        Assert.False(status.IsReachable);
    }


    [Fact]
    public void ReachabilityNeedsTheAppAndProximityOnTheSameNode()
    {
        // a nearby node with no app, plus an app-carrying node that is away: neither can take a live message, and
        // testing the two conditions across the whole list rather than per node would wrongly say it can
        var status = WearableNodeMerge.Build(
            [Node("phone-2", "Tablet")],
            [Node("watch-1", nearby: false)]
        );

        Assert.True(status.IsPaired);
        Assert.True(status.IsAppInstalled);
        Assert.False(status.IsReachable);
    }


    [Fact]
    public void TargetPrefersANearbyNodeOverACloudConnectedOne()
    {
        var status = WearableNodeMerge.Build(
            [Node("cloud", "Old Watch", nearby: false), Node("near", "New Watch")],
            [Node("cloud", nearby: false), Node("near", nearby: false)]
        );

        Assert.Equal("near", WearableNodeMerge.FindTarget(status)?.Id);
    }


    [Fact]
    public void TargetFallsBackToACloudConnectedNode()
    {
        var status = WearableNodeMerge.Build([Node("cloud", nearby: false)], [Node("cloud", nearby: false)]);

        Assert.Equal("cloud", WearableNodeMerge.FindTarget(status)?.Id);
    }


    [Fact]
    public void TargetNeverReturnsAWatchThatIsMerelyKnown()
    {
        // the node list now carries paired-but-away watches; handing one to MessageClient fails inside Play Services
        // with a far vaguer error than the NotReachable the caller gets instead
        var status = WearableNodeMerge.Build([], [Node("watch-1", nearby: false)]);

        Assert.Null(WearableNodeMerge.FindTarget(status));
    }


    [Fact]
    public void ANodeWithNoDisplayNameGetsAnEmptyOneRatherThanNull()
    {
        var status = WearableNodeMerge.Build([], [new RawWearableNode("watch-1", null, false)]);

        Assert.Equal(String.Empty, Assert.Single(status.Nodes).DisplayName);
    }
}
