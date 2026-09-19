namespace Shiny.Wearables;


/// <summary>
/// Receives what the wearable sends, including while the app is in the background — the platform wakes the app
/// for it (a WatchConnectivity delivery on iOS, the Data Layer's listener service on Android).
/// </summary>
public interface IWearableDelegate
{
    /// <summary>
    /// The wearable was paired, unpaired, installed or removed the companion app, or became reachable or unreachable.
    /// </summary>
    Task OnStatusChanged(WearableStatus status);

    /// <summary>
    /// A live message arrived. What this returns is the reply the wearable gets.
    /// <para>
    /// With several delegates registered, each is asked in turn and the first non-null reply wins; the wearable gets an
    /// empty reply if none answers.
    /// </para>
    /// </summary>
    Task<byte[]?> OnMessageReceived(WearableMessage message);

    /// <summary>
    /// The wearable shared new context.
    /// </summary>
    Task OnContextReceived(WearableContext context);

    /// <summary>
    /// A queued data transfer arrived.
    /// </summary>
    Task OnTransferReceived(WearableTransfer transfer);

    /// <summary>
    /// A file arrived. It is in the app's data directory and the delegate owns it.
    /// </summary>
    Task OnFileReceived(WearableFile file);

    /// <summary>
    /// An outgoing transfer or file transfer finished — delivered, failed, or cancelled.
    /// </summary>
    Task OnTransferCompleted(WearableTransferResult result);
}


/// <summary>
/// An <see cref="IWearableDelegate"/> that does nothing; override only what you need.
/// </summary>
public class WearableDelegate : IWearableDelegate
{
    /// <inheritdoc />
    public virtual Task OnStatusChanged(WearableStatus status) => Task.CompletedTask;
    /// <inheritdoc />
    public virtual Task<byte[]?> OnMessageReceived(WearableMessage message) => Task.FromResult<byte[]?>(null);
    /// <inheritdoc />
    public virtual Task OnContextReceived(WearableContext context) => Task.CompletedTask;
    /// <inheritdoc />
    public virtual Task OnTransferReceived(WearableTransfer transfer) => Task.CompletedTask;
    /// <inheritdoc />
    public virtual Task OnFileReceived(WearableFile file) => Task.CompletedTask;
    /// <inheritdoc />
    public virtual Task OnTransferCompleted(WearableTransferResult result) => Task.CompletedTask;
}
