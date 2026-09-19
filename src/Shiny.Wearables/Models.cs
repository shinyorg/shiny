namespace Shiny.Wearables;


/// <summary>
/// A connected wearable. On iOS this is the active Apple Watch; on Android it is a Wear OS node (or, in a Wear OS app,
/// the phone).
/// </summary>
/// <param name="Id">The node id. iOS has one active watch, so it is always <c>watch</c> there.</param>
/// <param name="DisplayName">The name the platform gives the node.</param>
/// <param name="IsNearby">Whether the node is directly connected (Bluetooth) rather than reached through the cloud.</param>
/// <param name="HasApp">Whether the node runs the companion app — on Wear OS, whether it advertises <see cref="WearableProtocol.Capability"/>.</param>
public sealed record WearableNode(string Id, string DisplayName, bool IsNearby, bool HasApp);


/// <summary>
/// Whether a companion wearable is there to talk to.
/// </summary>
/// <param name="IsSupported">False where the platform has no wearable API: not iOS or Android, an iPad, or an Android device without the Wear OS app / Google Play services.</param>
/// <param name="IsPaired">A wearable is paired (iOS) or connected (Android — the Data Layer does not report paired-but-disconnected watches).</param>
/// <param name="IsAppInstalled">The companion app is installed on the wearable.</param>
/// <param name="IsReachable">A live message can be sent right now. Context, transfers and files do not need this; they queue.</param>
/// <param name="Nodes">The wearables the platform reports.</param>
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
