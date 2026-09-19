# Shiny.Wearables API Reference

## Installation

```xml
<PackageReference Include="Shiny.Wearables" Version="5.*" />
```

Targets: `net10.0` (contracts only, nothing registered), `net10.0-android`, `net10.0-ios`.

## Namespaces

```csharp
using Shiny;            // AddWearables
using Shiny.Wearables;  // everything else
```

## Registration (namespace: Shiny)

```csharp
public static class WearablesServiceCollectionExtensions
{
    // IWearableManager on iOS/Android; nothing elsewhere
    IServiceCollection AddWearables(this IServiceCollection services);

    // + a delegate; call once per delegate
    IServiceCollection AddWearables<TDelegate>(this IServiceCollection services) where TDelegate : class, IWearableDelegate;
}
```

## IWearableManager

```csharp
public interface IWearableManager
{
    Task<WearableStatus> GetStatus(CancellationToken cancelToken = default);

    // live; throws WearableException(NotReachable) when no reachable wearable runs the companion app
    // nodeId: Wear OS only; null = the nearby node advertising "shiny_wearable"
    Task<byte[]> SendMessage(string path, byte[] data, string? nodeId = null, CancellationToken cancelToken = default);

    Task UpdateContext(byte[] data, CancellationToken cancelToken = default);
    Task<byte[]?> GetContext(CancellationToken cancelToken = default);                  // what this app last shared
    Task<WearableContext?> GetReceivedContext(CancellationToken cancelToken = default); // what the wearable last shared

    Task<string> Transfer(string path, byte[] data, CancellationToken cancelToken = default);   // returns transfer id
    Task<string> TransferFile(string path, string filePath, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancelToken = default);
    Task<IReadOnlyList<WearableTransferInfo>> GetPendingTransfers(CancellationToken cancelToken = default);
    Task<bool> CancelTransfer(string id, CancellationToken cancelToken = default);        // false when nothing pending has the id
}
```

`TransferFile` throws `FileNotFoundException` when the file is missing. Invalid paths throw `ArgumentException`.

## IWearableDelegate / WearableDelegate

```csharp
public interface IWearableDelegate
{
    Task OnStatusChanged(WearableStatus status);
    Task<byte[]?> OnMessageReceived(WearableMessage message);   // first non-null reply across delegates wins
    Task OnContextReceived(WearableContext context);
    Task OnTransferReceived(WearableTransfer transfer);
    Task OnFileReceived(WearableFile file);                     // file already moved into AppData; delegate owns it
    Task OnTransferCompleted(WearableTransferResult result);    // outgoing transfer/file delivered, failed or cancelled
}

public class WearableDelegate : IWearableDelegate { /* virtual no-ops; OnMessageReceived returns null */ }
```

## Models

```csharp
public sealed record WearableNode(string Id, string DisplayName, bool IsNearby, bool HasApp);

public sealed record WearableStatus(bool IsSupported, bool IsPaired, bool IsAppInstalled, bool IsReachable, IReadOnlyList<WearableNode> Nodes)
{
    public static WearableStatus NotSupported { get; }
}

public sealed record WearableMessage(string Path, byte[] Data, string? NodeId, bool ExpectsReply);
public sealed record WearableContext(byte[] Data, string? NodeId);
public sealed record WearableTransfer(string Id, string Path, byte[] Data, string? NodeId);
public sealed record WearableFile(string Id, string Path, string FileName, string LocalPath, IReadOnlyDictionary<string, string> Metadata, string? NodeId);

public enum WearableTransferKind { Data, File }
public sealed record WearableTransferInfo(string Id, string Path, WearableTransferKind Kind, double? Progress); // Progress: iOS files only
public sealed record WearableTransferResult(string Id, string Path, WearableTransferKind Kind, string? Error)
{
    public bool Succeeded { get; } // Error is null
}
```

## Errors

```csharp
public enum WearableErrorCode { NotSupported, NotReachable, Failed }
public class WearableException(WearableErrorCode code, string message, Exception? inner = null) : Exception
{
    public WearableErrorCode Code { get; }
}
```

## WearableProtocol (wire format)

```csharp
public static class WearableProtocol
{
    const string Capability     = "shiny_wearable";
    const string Prefix         = "/shiny";
    const string MessagePrefix  = "/shiny/message/";   // + app path
    const string ContextPath    = "/shiny/context";
    const string TransferPrefix = "/shiny/transfer/";  // + id
    const string FilePrefix     = "/shiny/file/";      // + id

    const string IdKey = "id", PathKey = "path", DataKey = "data", NameKey = "name", MetadataKey = "metadata", FileAssetKey = "file";

    static string NormalizePath(string path);        // "/a/b/" -> "a/b"; throws on empty, whitespace, '?', '#'
    static string ToMessagePath(string path);        // "sync" -> "/shiny/message/sync"
    static string? FromMessagePath(string? wirePath);
    static string NewTransferId();                   // Guid "N"
    static string GetInboxPath(string root, string transferId, string fileName); // {root}/Shiny.Wearables/{id}/{name}, traversal-safe
}
```

| Shape | watchOS (WatchConnectivity) | Wear OS (Data Layer) |
|---|---|---|
| Message | `sendMessage(["path","data"], replyHandler)`; reply `["data"]` | `sendRequest(node, "/shiny/message/{path}", bytes)`; reply = RPC result |
| Context | `updateApplicationContext(["data"])` | item `/shiny/context`, byte array `data` |
| Transfer | `transferUserInfo(["id","path","data"])` | urgent item `/shiny/transfer/{id}` (`id`,`path`,`data`), receiver deletes |
| File | `transferFile(url, metadata: ["id","path","name","metadata"])` | urgent item `/shiny/file/{id}` (`id`,`path`,`name`,`metadata`, asset `file`), receiver deletes |

## Android service

`ShinyWearableListenerService` (`WearableListenerService`, exported) - intent filters `MESSAGE_RECEIVED`, `REQUEST_RECEIVED`, `DATA_CHANGED`, `CAPABILITY_CHANGED`; data `wear://*` path prefix `/shiny`. Registered by attribute; forwards to the manager once Shiny has started.
