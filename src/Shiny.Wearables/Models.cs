namespace Shiny.Wearables;


/// <summary>
/// A wearable the platform knows about. On iOS this is the active Apple Watch; on Android it is a Wear OS node (or, in
/// a Wear OS app, the phone).
/// <para>
/// Being listed does not mean it can be reached: a watch that is paired but switched off, charging in another room or
/// out of Bluetooth range is still listed, with <see cref="IsConnected"/> false. That is the point — it is how
/// "the user has a watch, it just is not here" is told apart from "the user has no watch".
/// </para>
/// </summary>
/// <param name="Id">The node id. iOS has one active watch, so it is always <c>watch</c> there.</param>
/// <param name="DisplayName">The name the platform gives the node.</param>
/// <param name="IsConnected">
/// Whether the platform can reach the node at all — over Bluetooth, or through the cloud on Wear OS. False for a
/// wearable that is paired but away. Anything sent to a node that is not connected fails or waits in the queue.
/// </param>
/// <param name="IsNearby">
/// Whether the node is directly connected (Bluetooth) rather than reached through the cloud. Always false when
/// <see cref="IsConnected"/> is false. A cloud-connected Wear OS node can receive transfers but is far slower, which is
/// why live messages need this rather than <see cref="IsConnected"/>.
/// </param>
/// <param name="HasApp">Whether the node runs the companion app — on Wear OS, whether it advertises <see cref="WearableProtocol.Capability"/>.</param>
public sealed record WearableNode(string Id, string DisplayName, bool IsConnected, bool IsNearby, bool HasApp);


/// <summary>
/// Whether a companion wearable is there to talk to.
/// </summary>
/// <param name="IsSupported">
/// Whether this platform has a wearable API at all — false when it is not iOS or Android, on an iPad, or on an Android
/// device without the Wear OS app or Google Play services. <b>Not</b> "a watch is set up": that is
/// <see cref="IsPaired"/> and <see cref="IsAppInstalled"/>.
/// </param>
/// <param name="IsPaired">
/// A wearable is paired. This survives the wearable being switched off or out of range, so it is the flag for "this
/// user has a watch" rather than "the watch is here".
/// <para>
/// On Wear OS a watch is only known while it is connected <i>or</i> has previously advertised the companion app's
/// capability, so a paired watch that has never had the app installed and is currently away cannot be seen at all. The
/// Data Layer exposes nothing that would report it.
/// </para>
/// </param>
/// <param name="IsAppInstalled">The companion app is installed on the wearable. Like <see cref="IsPaired"/>, this survives the wearable being away.</param>
/// <param name="IsReachable">A live message can be sent right now. Context, transfers and files do not need this; they queue.</param>
/// <param name="Nodes">The wearables the platform knows about, connected or not.</param>
public sealed record WearableStatus(
    bool IsSupported,
    bool IsPaired,
    bool IsAppInstalled,
    bool IsReachable,
    IReadOnlyList<WearableNode> Nodes
)
{
    /// <summary>The status on a platform with no wearable API.</summary>
    public static WearableStatus NotSupported { get; } = new(false, false, false, false, []);

    /// <summary>
    /// A wearable is set up to talk to: paired, with the companion app installed. Says nothing about whether it is
    /// here right now — check <see cref="IsReachable"/> for that.
    /// </summary>
    /// <remarks>
    /// The flag to drive a "send to watch" feature being offered at all. It stays true while the watch is off or out
    /// of range, so the feature does not appear and disappear as the user moves around.
    /// </remarks>
    public bool IsConfigured => this.IsPaired && this.IsAppInstalled;
}


/// <summary>
/// A live message from the wearable.
/// </summary>
/// <param name="Path">The path the sender addressed, without the protocol's prefix — <c>sync</c>, <c>workout/start</c>.</param>
/// <param name="Data">The message body.</param>
/// <param name="NodeId">The node that sent it.</param>
/// <param name="ExpectsReply">Whether the sender is waiting on a reply. When it is not, what the delegate returns is discarded.</param>
public sealed record WearableMessage(string Path, byte[] Data, string? NodeId, bool ExpectsReply);


/// <summary>
/// The latest context a wearable shared. Only the newest value is kept; a newer one replaces it.
/// </summary>
public sealed record WearableContext(byte[] Data, string? NodeId);


/// <summary>
/// A queued data transfer from the wearable. Transfers are delivered in order, once, even if the app was not running
/// when they were sent.
/// </summary>
public sealed record WearableTransfer(string Id, string Path, byte[] Data, string? NodeId);


/// <summary>
/// A file the wearable transferred. The file has already been moved out of the platform's temporary location into
/// the app's data directory; the delegate owns it from here — move it, read it, or delete it.
/// </summary>
/// <param name="Id">The transfer id the sender assigned.</param>
/// <param name="Path">The path the sender addressed.</param>
/// <param name="FileName">The file's name as the sender had it.</param>
/// <param name="LocalPath">Where the file is now.</param>
/// <param name="Metadata">The string metadata sent with the file.</param>
/// <param name="NodeId">The node that sent it.</param>
public sealed record WearableFile(
    string Id,
    string Path,
    string FileName,
    string LocalPath,
    IReadOnlyDictionary<string, string> Metadata,
    string? NodeId
);


/// <summary>What an outgoing transfer carries.</summary>
public enum WearableTransferKind
{
    /// <summary>A queued data transfer — <see cref="IWearableManager.Transfer"/>.</summary>
    Data,

    /// <summary>A file transfer — <see cref="IWearableManager.TransferFile"/>.</summary>
    File
}


/// <summary>
/// An outgoing transfer the wearable has not received yet.
/// </summary>
/// <param name="Progress">0–1 where the platform reports it (iOS file transfers); null otherwise.</param>
public sealed record WearableTransferInfo(string Id, string Path, WearableTransferKind Kind, double? Progress);


/// <summary>
/// An outgoing transfer finished: delivered, failed, or cancelled.
/// </summary>
/// <param name="Error">Null when the wearable received it.</param>
public sealed record WearableTransferResult(string Id, string Path, WearableTransferKind Kind, string? Error)
{
    /// <summary>The wearable received it.</summary>
    public bool Succeeded => this.Error is null;
}
