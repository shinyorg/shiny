namespace Shiny.Wearables;


/// <summary>
/// Talks to the companion app on a paired Apple Watch (WatchConnectivity) or Wear OS device (the Data Layer). The same
/// API runs on the other end of a Wear OS pair, where the "wearable" is the phone.
/// <para>
/// Everything is bytes addressed by a path. How those map onto each platform — so a Swift or Kotlin companion app can
/// speak it — is <see cref="WearableProtocol"/>.
/// </para>
/// </summary>
public interface IWearableManager
{
    /// <summary>
    /// Whether a wearable is paired, has the companion app, and is reachable right now.
    /// </summary>
    Task<WearableStatus> GetStatus(CancellationToken cancelToken = default);

    /// <summary>
    /// Sends a live message and waits for the companion app's reply. The wearable has to be reachable; this does not
    /// queue.
    /// </summary>
    /// <param name="path">What the message is about — <c>sync</c>, <c>workout/start</c>. Leading and trailing slashes are ignored.</param>
    /// <param name="data">The body. Keep it small: iOS caps a message at about 64 KB, Wear OS at about 100 KB.</param>
    /// <param name="nodeId">Wear OS only: the node to send to. Null picks the nearby node that runs the companion app.</param>
    /// <returns>The reply's body; empty when the companion app replied with nothing.</returns>
    /// <exception cref="WearableException">Not supported, no reachable wearable, or the send failed.</exception>
    Task<byte[]> SendMessage(string path, byte[] data, string? nodeId = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Replaces the context shared with the wearable. Only the latest value is kept, and the wearable receives it
    /// the next time it can — use it for state ("the current plan", "settings"), not for events.
    /// </summary>
    Task UpdateContext(byte[] data, CancellationToken cancelToken = default);

    /// <summary>
    /// The context this app last shared, or null.
    /// </summary>
    Task<byte[]?> GetContext(CancellationToken cancelToken = default);

    /// <summary>
    /// The context the wearable last shared, or null.
    /// </summary>
    Task<WearableContext?> GetReceivedContext(CancellationToken cancelToken = default);

    /// <summary>
    /// Queues data for the wearable. Transfers are delivered in order and survive the wearable being out of range or
    /// the companion app not running.
    /// </summary>
    /// <returns>The transfer's id, as <see cref="WearableTransferInfo"/> and <see cref="WearableTransferResult"/> report it.</returns>
    Task<string> Transfer(string path, byte[] data, CancellationToken cancelToken = default);

    /// <summary>
    /// Queues a file for the wearable. The platform reads the file while it transfers, so leave it in place until
    /// <see cref="IWearableDelegate.OnTransferCompleted"/> reports it done.
    /// </summary>
    /// <param name="path">What the file is — <c>maps/offline</c>.</param>
    /// <param name="filePath">The local file to send.</param>
    /// <param name="metadata">String metadata the receiver gets with the file.</param>
    /// <returns>The transfer's id.</returns>
    Task<string> TransferFile(string path, string filePath, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Outgoing transfers and file transfers the wearable has not received yet.
    /// </summary>
    Task<IReadOnlyList<WearableTransferInfo>> GetPendingTransfers(CancellationToken cancelToken = default);

    /// <summary>
    /// Cancels an outgoing transfer the wearable has not received yet.
    /// </summary>
    /// <returns>False when no pending transfer has the id.</returns>
    Task<bool> CancelTransfer(string id, CancellationToken cancelToken = default);
}
