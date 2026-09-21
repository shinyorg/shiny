namespace Shiny.Wearables.Infrastructure;


/// <summary>
/// One node as a platform reported it, stripped of the platform's own type so the merge below can be tested without a
/// watch or a Wear OS emulator.
/// </summary>
/// <param name="Id">The node id.</param>
/// <param name="DisplayName">The name the platform gives it, if any.</param>
/// <param name="IsNearby">Whether it is directly connected over Bluetooth. Only meaningful for a connected node.</param>
internal readonly record struct RawWearableNode(string Id, string? DisplayName, bool IsNearby);


/// <summary>
/// Merges the two lists Wear OS answers separately - who is connected, and who has the companion app - into one node
/// list and the status derived from it.
/// </summary>
/// <remarks>
/// <para>This exists as a pure function because getting it wrong is silent and the failure only appears when a watch
/// goes out of range, which no unit test on the build machine would ever reach. It was wrong: the node list was built
/// from the connected nodes alone, so a paired watch that was charging in another room vanished from the status
/// entirely, taking <see cref="WearableStatus.IsPaired"/> and <see cref="WearableStatus.IsAppInstalled"/> with it, and
/// making "the user has a watch, it is just not here" read exactly like "the user has no watch".</para>
/// <para>Neither source is sufficient alone. <c>GetConnectedNodes</c> is the only one that says who is reachable now,
/// and cannot see a watch that is away. <c>CapabilityClient</c> with <c>FilterAll</c> is the only one that can see a
/// watch that is away, and cannot say whether it is reachable - nor see a paired watch that never had the app
/// installed, which the Data Layer exposes nothing to report.</para>
/// </remarks>
internal static class WearableNodeMerge
{
    /// <summary>
    /// Builds the status from the connected nodes and the nodes advertising the companion app's capability.
    /// </summary>
    /// <param name="connected">Nodes the Data Layer can reach right now, over Bluetooth or the cloud.</param>
    /// <param name="capable">Every node advertising the capability, connected or not.</param>
    public static WearableStatus Build(IEnumerable<RawWearableNode> connected, IEnumerable<RawWearableNode> capable)
    {
        var nodes = new Dictionary<string, WearableNode>(StringComparer.Ordinal);

        // the capability nodes go in first as the "known, but not here" baseline; a node that is also connected is
        // overwritten below with its live flags rather than being listed twice
        foreach (var node in capable)
            nodes[node.Id] = new WearableNode(node.Id, node.DisplayName ?? String.Empty, false, false, true);

        foreach (var node in connected)
        {
            // a connected node keeps its own display name: the capability entry's can be stale, and the connected one
            // is what the user renamed the watch to
            var hasApp = nodes.ContainsKey(node.Id);
            nodes[node.Id] = new WearableNode(node.Id, node.DisplayName ?? String.Empty, true, node.IsNearby, hasApp);
        }

        var all = nodes.Values.ToList();

        return new WearableStatus(
            true,
            all.Count > 0,
            all.Any(x => x.HasApp),

            // a live message needs a direct Bluetooth link; a cloud-connected node takes transfers but is far too slow
            // to be called reachable
            all.Any(x => x.HasApp && x.IsNearby),
            all
        );
    }


    /// <summary>
    /// The node a live message should be sent to: nearby first, then a cloud-connected one.
    /// </summary>
    /// <remarks>
    /// Never returns a node that is merely known. The node list carries paired-but-away watches now, and handing one of
    /// those to <c>MessageClient</c> fails inside Play Services with a far vaguer error than the
    /// <see cref="WearableErrorCode.NotReachable"/> the caller gets instead.
    /// </remarks>
    public static WearableNode? FindTarget(WearableStatus status)
        => status.Nodes.FirstOrDefault(x => x.HasApp && x.IsNearby)
           ?? status.Nodes.FirstOrDefault(x => x.HasApp && x.IsConnected);
}
