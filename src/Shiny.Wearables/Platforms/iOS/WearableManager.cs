using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WatchConnectivity;

namespace Shiny.Wearables;


/// <summary>
/// <see cref="IWearableManager"/> over WatchConnectivity. The session is activated at startup, not on first use:
/// WatchConnectivity delivers what arrived while the app was not running as soon as the session activates, and only
/// to a delegate that is already set.
/// </summary>
public class WearableManager(
    IServiceProvider services,
    IPlatform platform,
    ILogger<WearableManager> logger
) : IWearableManager, IShinyStartupTask
{
    const string WatchNodeId = "watch";
    static readonly TimeSpan ActivationTimeout = TimeSpan.FromSeconds(10);

    readonly TaskCompletionSource activation = new(TaskCreationOptions.RunContinuationsAsynchronously);
    WCSession? session;
    SessionDelegate? sessionDelegate;


    /// <inheritdoc />
    public void Start()
    {
        if (!WCSession.IsSupported)
        {
            logger.LogInformation("WatchConnectivity is not supported on this device");
            this.activation.TrySetResult();
            return;
        }

        this.session = WCSession.DefaultSession;
        this.sessionDelegate = new SessionDelegate(this);
        this.session.Delegate = this.sessionDelegate;
        this.session.ActivateSession();
    }


    /// <inheritdoc />
    public async Task<WearableStatus> GetStatus(CancellationToken cancelToken = default)
    {
        if (await this.GetActiveSession(cancelToken).ConfigureAwait(false) is not { } s)
            return WearableStatus.NotSupported;

        return ToStatus(s);
    }


    /// <inheritdoc />
    public async Task<byte[]> SendMessage(string path, byte[] data, string? nodeId = null, CancellationToken cancelToken = default)
    {
        path = WearableProtocol.NormalizePath(path);
        ArgumentNullException.ThrowIfNull(data);

        var s = await this.RequireSession(cancelToken).ConfigureAwait(false);
        if (!s.Reachable)
            throw new WearableException(WearableErrorCode.NotReachable, "The Apple Watch app is not reachable");

        var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = cancelToken.Register(() => tcs.TrySetCanceled(cancelToken));

        s.SendMessage(
            Dictionary(
                (WearableProtocol.PathKey, new NSString(path)),
                (WearableProtocol.DataKey, NSData.FromArray(data))
            ),
            reply => tcs.TrySetResult(GetData(reply) ?? []),
            error => tcs.TrySetException(new WearableException(WearableErrorCode.Failed, error.LocalizedDescription))
        );
        return await tcs.Task.ConfigureAwait(false);
    }


    /// <inheritdoc />
    public async Task UpdateContext(byte[] data, CancellationToken cancelToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        var s = await this.RequireSession(cancelToken).ConfigureAwait(false);
        if (!s.UpdateApplicationContext(Dictionary((WearableProtocol.DataKey, NSData.FromArray(data))), out var error))
            throw new WearableException(WearableErrorCode.Failed, error?.LocalizedDescription ?? "The application context was not updated");
    }


    /// <inheritdoc />
    public async Task<byte[]?> GetContext(CancellationToken cancelToken = default)
    {
        var s = await this.RequireSession(cancelToken).ConfigureAwait(false);
        return GetData(s.ApplicationContext);
    }


    /// <inheritdoc />
    public async Task<WearableContext?> GetReceivedContext(CancellationToken cancelToken = default)
    {
        var s = await this.RequireSession(cancelToken).ConfigureAwait(false);
        return GetData(s.ReceivedApplicationContext) is { } data ? new WearableContext(data, WatchNodeId) : null;
    }


    /// <inheritdoc />
    public async Task<string> Transfer(string path, byte[] data, CancellationToken cancelToken = default)
    {
        path = WearableProtocol.NormalizePath(path);
        ArgumentNullException.ThrowIfNull(data);

        var s = await this.RequireSession(cancelToken).ConfigureAwait(false);
        var id = WearableProtocol.NewTransferId();
        s.TransferUserInfo(Dictionary(
            (WearableProtocol.IdKey, new NSString(id)),
            (WearableProtocol.PathKey, new NSString(path)),
            (WearableProtocol.DataKey, NSData.FromArray(data))
        ));
        return id;
    }


    /// <inheritdoc />
    public async Task<string> TransferFile(string path, string filePath, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancelToken = default)
    {
        path = WearableProtocol.NormalizePath(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("The file to transfer does not exist", filePath);

        var s = await this.RequireSession(cancelToken).ConfigureAwait(false);
        var id = WearableProtocol.NewTransferId();

        var meta = new NSMutableDictionary<NSString, NSObject>();
        foreach (var pair in metadata ?? new Dictionary<string, string>())
            meta[new NSString(pair.Key)] = new NSString(pair.Value);

        s.TransferFile(
            NSUrl.CreateFileUrl(filePath, null),
            Dictionary(
                (WearableProtocol.IdKey, new NSString(id)),
                (WearableProtocol.PathKey, new NSString(path)),
                (WearableProtocol.NameKey, new NSString(Path.GetFileName(filePath))),
                (WearableProtocol.MetadataKey, meta)
            )
        );
        return id;
    }


    /// <inheritdoc />
    public async Task<IReadOnlyList<WearableTransferInfo>> GetPendingTransfers(CancellationToken cancelToken = default)
    {
        var s = await this.RequireSession(cancelToken).ConfigureAwait(false);
        var list = new List<WearableTransferInfo>();

        foreach (var t in s.OutstandingUserInfoTransfers)
        {
            if (GetString(t.UserInfo, WearableProtocol.IdKey) is { } id)
                list.Add(new(id, GetString(t.UserInfo, WearableProtocol.PathKey) ?? "", WearableTransferKind.Data, null));
        }
        foreach (var t in s.OutstandingFileTransfers)
        {
            if (GetString(t.File.Metadata, WearableProtocol.IdKey) is { } id)
                list.Add(new(id, GetString(t.File.Metadata, WearableProtocol.PathKey) ?? "", WearableTransferKind.File, t.Progress.FractionCompleted));
        }
        return list;
    }


    /// <inheritdoc />
    public async Task<bool> CancelTransfer(string id, CancellationToken cancelToken = default)
    {
        var s = await this.RequireSession(cancelToken).ConfigureAwait(false);

        foreach (var t in s.OutstandingUserInfoTransfers)
        {
            if (GetString(t.UserInfo, WearableProtocol.IdKey) == id)
            {
                t.Cancel();
                return true;
            }
        }
        foreach (var t in s.OutstandingFileTransfers)
        {
            if (GetString(t.File.Metadata, WearableProtocol.IdKey) == id)
            {
                t.Cancel();
                return true;
            }
        }
        return false;
    }


    async Task<WCSession?> GetActiveSession(CancellationToken cancelToken)
    {
        if (this.session == null)
            return null;

        try
        {
            await this.activation.Task.WaitAsync(ActivationTimeout, cancelToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            // Answer with what the session knows; its properties read as unpaired until activation completes.
            logger.LogWarning("WatchConnectivity has not finished activating");
        }
        return this.session;
    }


    async Task<WCSession> RequireSession(CancellationToken cancelToken)
        => await this.GetActiveSession(cancelToken).ConfigureAwait(false)
           ?? throw new WearableException(WearableErrorCode.NotSupported, "WatchConnectivity is not supported on this device");


    static WearableStatus ToStatus(WCSession s)
    {
        IReadOnlyList<WearableNode> nodes = s.Paired
            ? [new WearableNode(WatchNodeId, "Apple Watch", s.Reachable, s.WatchAppInstalled)]
            : [];

        return new WearableStatus(true, s.Paired, s.WatchAppInstalled, s.Reachable, nodes);
    }


    static NSDictionary<NSString, NSObject> Dictionary(params (string Key, NSObject Value)[] pairs)
    {
        var dict = new NSMutableDictionary<NSString, NSObject>();
        foreach (var (key, value) in pairs)
            dict[new NSString(key)] = value;

        return NSDictionary<NSString, NSObject>.FromObjectsAndKeys(dict.Values, dict.Keys, (nint)dict.Count);
    }


    static byte[]? GetData(NSDictionary<NSString, NSObject>? dict)
        => dict?.ObjectForKey(new NSString(WearableProtocol.DataKey)) is NSData data ? data.ToArray() : null;


    static string? GetString(NSDictionary<NSString, NSObject>? dict, string key)
        => dict?.ObjectForKey(new NSString(key)) is NSString value ? value.ToString() : null;


    #region Session callbacks

    void OnActivated(WCSessionActivationState state, NSError? error)
    {
        if (error != null)
            logger.LogWarning("WatchConnectivity activation failed: {Error}", error.LocalizedDescription);

        this.activation.TrySetResult();
        this.RaiseStatus();
    }


    void OnDeactivated()
    {
        // The user switched watches: Apple's guidance is to activate again for the new one.
        this.session?.ActivateSession();
    }


    void RaiseStatus()
    {
        if (this.session is not { } s || s.ActivationState != WCSessionActivationState.Activated)
            return;

        var status = ToStatus(s);
        _ = services.RunDelegates<IWearableDelegate>(x => x.OnStatusChanged(status), logger);
    }


    void OnMessage(NSDictionary<NSString, NSObject> message, WCSessionReplyHandler? reply)
    {
        var path = GetString(message, WearableProtocol.PathKey);
        if (path == null)
        {
            logger.LogWarning("A watch message without a path was ignored");
            reply?.Invoke(Dictionary());
            return;
        }

        var msg = new WearableMessage(path, GetData(message) ?? [], WatchNodeId, reply != null);
        _ = this.ReplyAsync(msg, reply);
    }


    async Task ReplyAsync(WearableMessage message, WCSessionReplyHandler? reply)
    {
        var data = await WearableDelegates.GetReply(services, message, logger).ConfigureAwait(false);
        reply?.Invoke(Dictionary((WearableProtocol.DataKey, NSData.FromArray(data))));
    }


    void OnContext(NSDictionary<NSString, NSObject> context)
    {
        if (GetData(context) is not { } data)
            return;

        var ctx = new WearableContext(data, WatchNodeId);
        _ = services.RunDelegates<IWearableDelegate>(x => x.OnContextReceived(ctx), logger);
    }


    void OnUserInfo(NSDictionary<NSString, NSObject> userInfo)
    {
        var transfer = new WearableTransfer(
            GetString(userInfo, WearableProtocol.IdKey) ?? WearableProtocol.NewTransferId(),
            GetString(userInfo, WearableProtocol.PathKey) ?? "",
            GetData(userInfo) ?? [],
            WatchNodeId
        );
        _ = services.RunDelegates<IWearableDelegate>(x => x.OnTransferReceived(transfer), logger);
    }


    void OnFile(WCSessionFile file)
    {
        // WatchConnectivity deletes the file when this callback returns, so it is moved before anything is awaited.
        var meta = file.Metadata;
        var id = GetString(meta, WearableProtocol.IdKey) ?? WearableProtocol.NewTransferId();
        var name = GetString(meta, WearableProtocol.NameKey) ?? file.FileUrl.LastPathComponent ?? "file";
        var target = WearableProtocol.GetInboxPath(platform.AppData.FullName, id, name);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Move(file.FileUrl.Path!, target, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not keep the file the watch sent");
            return;
        }

        var metadata = new Dictionary<string, string>();
        if (meta?.ObjectForKey(new NSString(WearableProtocol.MetadataKey)) is NSDictionary dict)
        {
            foreach (var key in dict.Keys)
            {
                if (dict[key] is NSString value)
                    metadata[key.ToString()] = value.ToString();
            }
        }

        var received = new WearableFile(id, GetString(meta, WearableProtocol.PathKey) ?? "", name, target, metadata, WatchNodeId);
        _ = services.RunDelegates<IWearableDelegate>(x => x.OnFileReceived(received), logger);
    }


    void OnFinished(NSDictionary<NSString, NSObject>? info, WearableTransferKind kind, NSError? error)
    {
        if (GetString(info, WearableProtocol.IdKey) is not { } id)
            return; // not one of ours

        var result = new WearableTransferResult(id, GetString(info, WearableProtocol.PathKey) ?? "", kind, error?.LocalizedDescription);
        _ = services.RunDelegates<IWearableDelegate>(x => x.OnTransferCompleted(result), logger);
    }


    sealed class SessionDelegate(WearableManager manager) : WCSessionDelegate
    {
        public override void ActivationDidComplete(WCSession session, WCSessionActivationState activationState, NSError? error)
            => manager.OnActivated(activationState, error);

        public override void DidBecomeInactive(WCSession session) { }

        public override void DidDeactivate(WCSession session) => manager.OnDeactivated();

        public override void SessionWatchStateDidChange(WCSession session) => manager.RaiseStatus();

        public override void SessionReachabilityDidChange(WCSession session) => manager.RaiseStatus();

        public override void DidReceiveMessage(WCSession session, NSDictionary<NSString, NSObject> message)
            => manager.OnMessage(message, null);

        public override void DidReceiveMessage(WCSession session, NSDictionary<NSString, NSObject> message, WCSessionReplyHandler replyHandler)
            => manager.OnMessage(message, replyHandler);

        public override void DidReceiveApplicationContext(WCSession session, NSDictionary<NSString, NSObject> applicationContext)
            => manager.OnContext(applicationContext);

        public override void DidReceiveUserInfo(WCSession session, NSDictionary<NSString, NSObject> userInfo)
            => manager.OnUserInfo(userInfo);

        public override void DidReceiveFile(WCSession session, WCSessionFile file)
            => manager.OnFile(file);

        public override void DidFinishUserInfoTransfer(WCSession session, WCSessionUserInfoTransfer userInfoTransfer, NSError? error)
            => manager.OnFinished(userInfoTransfer.UserInfo, WearableTransferKind.Data, error);

        public override void DidFinishFileTransfer(WCSession session, WCSessionFileTransfer fileTransfer, NSError? error)
            => manager.OnFinished(fileTransfer.File.Metadata, WearableTransferKind.File, error);
    }

    #endregion
}
