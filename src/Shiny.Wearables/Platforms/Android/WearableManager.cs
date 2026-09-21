using Android.Gms.Common.Apis;
using Shiny.Wearables.Infrastructure;
using Android.Gms.Extensions;
using Android.Gms.Wearable;
using Android.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AndroidUri = Android.Net.Uri;

namespace Shiny.Wearables;


/// <summary>
/// <see cref="IWearableManager"/> over the Wear OS Data Layer. Messages are RPCs (<c>MessageClient.sendRequest</c>);
/// context, transfers and files are data items, which the Data Layer syncs whenever the nodes can reach each other.
/// Everything inbound arrives through <see cref="ShinyWearableListenerService"/>.
/// </summary>
public class WearableManager(
    AndroidPlatform platform,
    IServiceProvider services,
    ILogger<WearableManager> logger
) : IWearableManager, IShinyStartupTask
{
    // CommonStatusCodes.API_NOT_CONNECTED / ApiException for a device without the Wear OS app
    const int ApiUnavailable = 17;

    string? localNodeId;

    DataClient Data => WearableClass.GetDataClient(platform.AppContext);
    MessageClient Messages => WearableClass.GetMessageClient(platform.AppContext);
    NodeClient Nodes => WearableClass.GetNodeClient(platform.AppContext);
    CapabilityClient Capabilities => WearableClass.GetCapabilityClient(platform.AppContext);


    /// <inheritdoc />
    public void Start()
    {
        ShinyWearableListenerService.Manager = this;
        _ = this.AdvertiseAsync();
    }


    async Task AdvertiseAsync()
    {
        // The companion app finds this one by the capability, so it is advertised on every start rather than asking
        // the app to declare it in wear.xml.
        try
        {
            await this.Capabilities.AddLocalCapability(WearableProtocol.Capability).AsAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
            logger.LogInformation("The Wear OS Data Layer is not available on this device");
        }
        catch (Exception ex)
        {
            // Advertising a capability that is already advertised fails; that is fine.
            logger.LogDebug(ex, "Could not advertise the {Capability} capability", WearableProtocol.Capability);
        }
    }


    /// <inheritdoc />
    /// <remarks>
    /// <para>The node list is the <b>union</b> of two lists the Data Layer answers separately, because neither alone
    /// gives the right status.</para>
    /// <para><c>GetConnectedNodes</c> reports only what is reachable this instant, so a watch charging in another room
    /// disappears from it entirely - taking <see cref="WearableStatus.IsPaired"/> and
    /// <see cref="WearableStatus.IsAppInstalled"/> with it, and leaving "the user has a watch, it is just not here"
    /// indistinguishable from "the user has no watch". An app gating a "send to watch" feature on that would show and
    /// hide the feature as the user walked around the house.</para>
    /// <para><c>CapabilityClient</c> with <c>FilterAll</c> reports every node in the user's Wear network advertising
    /// the companion app's capability, connected or not - the persistent half - but says nothing about which are
    /// reachable now, and cannot see a paired watch that never had the app installed.</para>
    /// <para>So both are read and merged by <see cref="WearableNodeMerge"/>: the capability list supplies the
    /// paired-but-away watches, the connected list supplies liveness.</para>
    /// </remarks>
    public async Task<WearableStatus> GetStatus(CancellationToken cancelToken = default)
    {
        try
        {
            var connected = await this.Nodes.GetConnectedNodesAsync().WaitAsync(cancelToken).ConfigureAwait(false);
            var capable = await this.GetCapableNodes(cancelToken).ConfigureAwait(false);

            return WearableNodeMerge.Build(
                connected.Where(x => x?.Id != null).Select(x => new RawWearableNode(x.Id, x.DisplayName, x.IsNearby)),
                capable.Where(x => x?.Id != null).Select(x => new RawWearableNode(x.Id, x.DisplayName, false))
            );
        }
        catch (Exception ex) when (IsUnavailable(ex))
        {
            return WearableStatus.NotSupported;
        }
    }


    /// <inheritdoc />
    public async Task<byte[]> SendMessage(string path, byte[] data, string? nodeId = null, CancellationToken cancelToken = default)
    {
        var wirePath = WearableProtocol.ToMessagePath(path);
        ArgumentNullException.ThrowIfNull(data);

        nodeId ??= await this.FindReachableNode(cancelToken).ConfigureAwait(false);

        try
        {
            var result = await this.Messages
                .SendRequest(nodeId, wirePath, data)
                .AsAsync<Java.Lang.Object>()
                .WaitAsync(cancelToken)
                .ConfigureAwait(false);

            return result == null ? [] : JNIEnv.GetArray<byte>(result.Handle) ?? [];
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not WearableException)
        {
            throw Wrap(ex, "The message was not delivered");
        }
    }


    /// <inheritdoc />
    public async Task UpdateContext(byte[] data, CancellationToken cancelToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);

        var request = PutDataMapRequest.Create(WearableProtocol.ContextPath);
        request.DataMap.PutByteArray(WearableProtocol.DataKey, data);
        await this.Put(request.SetUrgent().AsPutDataRequest(), cancelToken).ConfigureAwait(false);
    }


    /// <inheritdoc />
    public async Task<byte[]?> GetContext(CancellationToken cancelToken = default)
    {
        var local = await this.GetLocalNodeId(cancelToken).ConfigureAwait(false);
        var items = await this.GetItems(WearableProtocol.ContextPath, false, cancelToken).ConfigureAwait(false);
        return items.FirstOrDefault(x => x.NodeId == local)?.Map.GetByteArray(WearableProtocol.DataKey);
    }


    /// <inheritdoc />
    public async Task<WearableContext?> GetReceivedContext(CancellationToken cancelToken = default)
    {
        var local = await this.GetLocalNodeId(cancelToken).ConfigureAwait(false);
        var items = await this.GetItems(WearableProtocol.ContextPath, false, cancelToken).ConfigureAwait(false);
        var item = items.FirstOrDefault(x => x.NodeId != local);
        return item?.Map.GetByteArray(WearableProtocol.DataKey) is { } data ? new WearableContext(data, item.NodeId) : null;
    }


    /// <inheritdoc />
    public async Task<string> Transfer(string path, byte[] data, CancellationToken cancelToken = default)
    {
        path = WearableProtocol.NormalizePath(path);
        ArgumentNullException.ThrowIfNull(data);

        var id = WearableProtocol.NewTransferId();
        var request = PutDataMapRequest.Create(WearableProtocol.TransferPrefix + id);
        request.DataMap.PutString(WearableProtocol.IdKey, id);
        request.DataMap.PutString(WearableProtocol.PathKey, path);
        request.DataMap.PutByteArray(WearableProtocol.DataKey, data);

        await this.Put(request.SetUrgent().AsPutDataRequest(), cancelToken).ConfigureAwait(false);
        return id;
    }


    /// <inheritdoc />
    public async Task<string> TransferFile(string path, string filePath, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancelToken = default)
    {
        path = WearableProtocol.NormalizePath(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("The file to transfer does not exist", filePath);

        var id = WearableProtocol.NewTransferId();
        var meta = new DataMap();
        foreach (var pair in metadata ?? new Dictionary<string, string>())
            meta.PutString(pair.Key, pair.Value);

        var request = PutDataMapRequest.Create(WearableProtocol.FilePrefix + id);
        request.DataMap.PutString(WearableProtocol.IdKey, id);
        request.DataMap.PutString(WearableProtocol.PathKey, path);
        request.DataMap.PutString(WearableProtocol.NameKey, Path.GetFileName(filePath));
        request.DataMap.PutDataMap(WearableProtocol.MetadataKey, meta);

        // A file descriptor rather than a file:// URI: Play services runs in another process, which cannot open a path
        // inside this app's private storage.
        using var fd = Android.OS.ParcelFileDescriptor.Open(new Java.IO.File(filePath), Android.OS.ParcelFileMode.ReadOnly)!;
        request.DataMap.PutAsset(WearableProtocol.FileAssetKey, Asset.CreateFromFd(fd));

        await this.Put(request.SetUrgent().AsPutDataRequest(), cancelToken).ConfigureAwait(false);
        return id;
    }


    /// <inheritdoc />
    public async Task<IReadOnlyList<WearableTransferInfo>> GetPendingTransfers(CancellationToken cancelToken = default)
    {
        var local = await this.GetLocalNodeId(cancelToken).ConfigureAwait(false);
        var list = new List<WearableTransferInfo>();

        foreach (var (prefix, kind) in new[] { (WearableProtocol.TransferPrefix, WearableTransferKind.Data), (WearableProtocol.FilePrefix, WearableTransferKind.File) })
        {
            var items = await this.GetItems(prefix, true, cancelToken, local).ConfigureAwait(false);
            list.AddRange(items.Select(x => new WearableTransferInfo(
                x.Map.GetString(WearableProtocol.IdKey) ?? x.Path[prefix.Length..],
                x.Map.GetString(WearableProtocol.PathKey) ?? "",
                kind,
                null
            )));
        }
        return list;
    }


    /// <inheritdoc />
    public async Task<bool> CancelTransfer(string id, CancellationToken cancelToken = default)
    {
        var local = await this.GetLocalNodeId(cancelToken).ConfigureAwait(false);
        var deleted = 0;

        foreach (var prefix in new[] { WearableProtocol.TransferPrefix, WearableProtocol.FilePrefix })
        {
            var count = await this.Data
                .DeleteDataItems(ItemUri(local, prefix + id))
                .AsAsync<Java.Lang.Integer>()
                .WaitAsync(cancelToken)
                .ConfigureAwait(false);

            deleted += count?.IntValue() ?? 0;
        }
        return deleted > 0;
    }


    #region Inbound (from ShinyWearableListenerService)

    internal void OnMessage(string path, byte[] data, string nodeId)
    {
        if (WearableProtocol.FromMessagePath(path) is not { } appPath)
            return;

        var message = new WearableMessage(appPath, data, nodeId, false);
        _ = WearableDelegates.GetReply(services, message, logger);
    }


    internal void OnRequest(string nodeId, string path, byte[] data, Android.Gms.Tasks.TaskCompletionSource reply)
    {
        if (WearableProtocol.FromMessagePath(path) is not { } appPath)
        {
            reply.SetException(new Java.Lang.IllegalArgumentException($"{path} is not a Shiny message path"));
            return;
        }
        _ = this.ReplyAsync(new WearableMessage(appPath, data, nodeId, true), reply);
    }


    async Task ReplyAsync(WearableMessage message, Android.Gms.Tasks.TaskCompletionSource reply)
    {
        var bytes = await WearableDelegates.GetReply(services, message, logger).ConfigureAwait(false);
        reply.SetResult(new Java.Lang.Object(JNIEnv.NewArray(bytes), JniHandleOwnership.TransferLocalRef));
    }


    internal void OnCapabilityChanged() => _ = this.RaiseStatusAsync();


    async Task RaiseStatusAsync()
    {
        var status = await this.GetStatus().ConfigureAwait(false);
        await services.RunDelegates<IWearableDelegate>(x => x.OnStatusChanged(status), logger).ConfigureAwait(false);
    }


    internal void OnDataChanged(DataEventBuffer buffer)
    {
        // The buffer is released when the service callback returns, so everything is read out of it first.
        var changes = new List<(int Type, string NodeId, string Path, DataMap? Map)>();
        foreach (var ev in buffer)
        {
            var item = ev.DataItem;
            var uri = item.Uri;
            var map = ev.Type == DataEvent.TypeChanged ? DataMapItem.FromDataItem(item).DataMap : null;
            changes.Add((ev.Type, uri.Host ?? "", uri.Path ?? "", map));
        }
        _ = this.ProcessChangesAsync(changes);
    }


    async Task ProcessChangesAsync(List<(int Type, string NodeId, string Path, DataMap? Map)> changes)
    {
        string local;
        try
        {
            local = await this.GetLocalNodeId(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not resolve the local Wear OS node");
            return;
        }

        foreach (var change in changes)
        {
            try
            {
                var mine = change.NodeId == local;
                if (change.Type == DataEvent.TypeDeleted)
                {
                    // The receiver deletes a transfer once it has it: that is the sender's delivery receipt.
                    if (mine)
                        await this.RaiseCompleted(change.Path).ConfigureAwait(false);
                }
                else if (!mine && change.Map != null)
                {
                    await this.Receive(change.NodeId, change.Path, change.Map).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process wearable data at {Path}", change.Path);
            }
        }
    }


    async Task Receive(string nodeId, string path, DataMap map)
    {
        if (path == WearableProtocol.ContextPath)
        {
            var ctx = new WearableContext(map.GetByteArray(WearableProtocol.DataKey) ?? [], nodeId);
            await services.RunDelegates<IWearableDelegate>(x => x.OnContextReceived(ctx), logger).ConfigureAwait(false);
        }
        else if (path.StartsWith(WearableProtocol.TransferPrefix, StringComparison.Ordinal))
        {
            var transfer = new WearableTransfer(
                map.GetString(WearableProtocol.IdKey) ?? path[WearableProtocol.TransferPrefix.Length..],
                map.GetString(WearableProtocol.PathKey) ?? "",
                map.GetByteArray(WearableProtocol.DataKey) ?? [],
                nodeId
            );
            await services.RunDelegates<IWearableDelegate>(x => x.OnTransferReceived(transfer), logger).ConfigureAwait(false);
            await this.Delete(nodeId, path).ConfigureAwait(false);
        }
        else if (path.StartsWith(WearableProtocol.FilePrefix, StringComparison.Ordinal))
        {
            var id = map.GetString(WearableProtocol.IdKey) ?? path[WearableProtocol.FilePrefix.Length..];
            var name = map.GetString(WearableProtocol.NameKey) ?? "file";
            var asset = map.GetAsset(WearableProtocol.FileAssetKey);
            if (asset == null)
            {
                logger.LogWarning("Wearable file {Id} arrived without its asset", id);
                return;
            }

            var target = WearableProtocol.GetInboxPath(platform.AppData.FullName, id, name);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

            var fd = await this.Data.GetFdForAsset(asset).AsAsync<DataClient.GetFdForAssetResponse>().ConfigureAwait(false);
            using (var input = fd.InputStream)
            await using (var output = File.Create(target))
                await input.CopyToAsync(output).ConfigureAwait(false);
            fd.Release();

            var metadata = new Dictionary<string, string>();
            if (map.GetDataMap(WearableProtocol.MetadataKey) is { } meta)
            {
                foreach (var key in meta.KeySet())
                {
                    if (meta.GetString(key) is { } value)
                        metadata[key] = value;
                }
            }

            var file = new WearableFile(id, map.GetString(WearableProtocol.PathKey) ?? "", name, target, metadata, nodeId);
            await services.RunDelegates<IWearableDelegate>(x => x.OnFileReceived(file), logger).ConfigureAwait(false);
            await this.Delete(nodeId, path).ConfigureAwait(false);
        }
    }


    async Task RaiseCompleted(string path)
    {
        WearableTransferKind kind;
        string id;
        if (path.StartsWith(WearableProtocol.TransferPrefix, StringComparison.Ordinal))
        {
            kind = WearableTransferKind.Data;
            id = path[WearableProtocol.TransferPrefix.Length..];
        }
        else if (path.StartsWith(WearableProtocol.FilePrefix, StringComparison.Ordinal))
        {
            kind = WearableTransferKind.File;
            id = path[WearableProtocol.FilePrefix.Length..];
        }
        else
        {
            return;
        }

        // The item is gone, so its path is not known here; the id is what the sender tracks.
        var result = new WearableTransferResult(id, "", kind, null);
        await services.RunDelegates<IWearableDelegate>(x => x.OnTransferCompleted(result), logger).ConfigureAwait(false);
    }

    #endregion


    async Task<string> FindReachableNode(CancellationToken cancelToken)
    {
        var status = await this.GetStatus(cancelToken).ConfigureAwait(false);
        if (!status.IsSupported)
            throw new WearableException(WearableErrorCode.NotSupported, "The Wear OS Data Layer is not available on this device");

        var node = WearableNodeMerge.FindTarget(status);

        return node?.Id ?? throw new WearableException(WearableErrorCode.NotReachable, "No connected Wear OS device runs the companion app");
    }


    /// <summary>
    /// Every node in the user's Wear network advertising the companion app's capability, whether or not it is connected
    /// right now.
    /// </summary>
    /// <remarks>
    /// <c>FilterAll</c> rather than <c>FilterReachable</c> is the whole point: the reachable filter answers the question
    /// <c>GetConnectedNodes</c> already answers and loses the paired-but-away watches this exists to find. The nodes
    /// carry their display names, so a watch that is away can still be named in the UI.
    /// </remarks>
    async Task<ICollection<INode>> GetCapableNodes(CancellationToken cancelToken)
    {
        var info = await this.Capabilities
            .GetCapabilityAsync(WearableProtocol.Capability, CapabilityClient.FilterAll)
            .WaitAsync(cancelToken)
            .ConfigureAwait(false);

        return info?.Nodes ?? [];
    }


    async Task<string> GetLocalNodeId(CancellationToken cancelToken)
    {
        if (this.localNodeId != null)
            return this.localNodeId;

        try
        {
            var node = await this.Nodes.GetLocalNodeAsync().WaitAsync(cancelToken).ConfigureAwait(false);
            return this.localNodeId = node.Id;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw Wrap(ex, "The Wear OS Data Layer is not available");
        }
    }


    async Task Put(PutDataRequest request, CancellationToken cancelToken)
    {
        try
        {
            await this.Data.PutDataItemAsync(request).WaitAsync(cancelToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw Wrap(ex, "The Data Layer did not accept the item");
        }
    }


    async Task Delete(string nodeId, string path)
    {
        try
        {
            await this.Data.DeleteDataItemsAsync(ItemUri(nodeId, path)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not acknowledge wearable data at {Path}", path);
        }
    }


    async Task<List<DataItemEntry>> GetItems(string path, bool prefix, CancellationToken cancelToken, string? nodeId = null)
    {
        var uri = ItemUri(nodeId ?? "*", path);
        var list = new List<DataItemEntry>();

        try
        {
            using var buffer = await this.Data
                .GetDataItemsAsync(uri, prefix ? DataClient.FilterPrefix : DataClient.FilterLiteral)
                .WaitAsync(cancelToken)
                .ConfigureAwait(false);

            foreach (var item in buffer)
                list.Add(new(item.Uri.Host ?? "", item.Uri.Path ?? "", DataMapItem.FromDataItem(item).DataMap));

            buffer.Release();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw Wrap(ex, "Could not read from the Data Layer");
        }
        return list;
    }


    sealed record DataItemEntry(string NodeId, string Path, DataMap Map);


    static AndroidUri ItemUri(string nodeId, string path)
        => new AndroidUri.Builder().Scheme(PutDataRequest.WearUriScheme)!.Authority(nodeId)!.Path(path)!.Build()!;


    static bool IsUnavailable(Exception ex)
        => ex is ApiException { StatusCode: ApiUnavailable } || ex.InnerException is ApiException { StatusCode: ApiUnavailable };


    static WearableException Wrap(Exception ex, string message)
        => IsUnavailable(ex)
            ? new WearableException(WearableErrorCode.NotSupported, "The Wear OS Data Layer is not available on this device", ex)
            : new WearableException(WearableErrorCode.Failed, $"{message}: {ex.Message}", ex);
}
