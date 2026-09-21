---
name: shiny-wearables
description: Guide for talking to a companion Apple Watch (WatchConnectivity) or Wear OS (Data Layer) app from .NET MAUI / .NET using Shiny.Wearables - live messages with replies, shared context, queued data and file transfers, and background delivery through a delegate
auto_invoke: true
triggers:
  - watch paired
  - watch connected
  - is the watch connected
  - watch reachable
  - watch out of range
  - paired but disconnected
  - watch status indicator
  - IsConfigured
  - IsConnected
  - WearableNode
  - WearableStatus
  - OnStatusChanged
  - wearable
  - wearables
  - apple watch
  - watchos
  - watch app
  - wear os
  - wearos
  - android wear
  - smartwatch
  - companion watch app
  - WatchConnectivity
  - WCSession
  - Wear OS Data Layer
  - MessageClient
  - DataClient
  - IWearableManager
  - IWearableDelegate
  - WearableDelegate
  - WearableProtocol
  - WearableException
  - WearableErrorCode
  - WearableStatus
  - WearableMessage
  - WearableContext
  - WearableTransfer
  - WearableFile
  - WearableTransferInfo
  - WearableTransferResult
  - ShinyWearableListenerService
  - AddWearables
  - shiny_wearable
  - Shiny.Wearables
  - shiny wearables
---

# Shiny Wearables

## When to Use This Skill

Use this skill when the user needs to:
- Send data or commands from a phone app to a companion Apple Watch or Wear OS app (or from a .NET Wear OS app to the phone)
- Ask the watch something and await its reply
- Share the latest state (settings, the current plan) with the watch
- Queue data or files for the watch that must arrive even if it is out of range
- Receive messages, context, transfers or files from the watch, including in the background
- Check whether a watch is paired, has the companion app installed, and is reachable
- Write the Swift (watchOS) or Kotlin (Wear OS) side so it speaks Shiny's wire format

## Library Overview

| Item | Value |
|------|-------|
| **NuGet** | `Shiny.Wearables` |
| **Primary Namespace** | `Shiny.Wearables` |
| **Config Namespace** | `Shiny` (`AddWearables` extension on `IServiceCollection`) |
| **Platforms** | iOS (WatchConnectivity), Android (Wear OS Data Layer - phone or a .NET Wear OS app). Other targets register nothing. |

### The four shapes

| Shape | API | Needs reachable? | Semantics |
|---|---|---|---|
| Message | `SendMessage(path, data)` → reply bytes | **Yes** - throws `WearableException(NotReachable)` otherwise | Live request/reply. ~64 KB on iOS, ~100 KB on Wear OS |
| Context | `UpdateContext(data)`, `GetContext()`, `GetReceivedContext()` | No | Only the newest value is kept, each direction |
| Transfer | `Transfer(path, data)` → id | No | Queued, in order, delivered once |
| File | `TransferFile(path, filePath, metadata)` → id | No | Queued; the platform reads the file during transfer - don't delete it until `OnTransferCompleted` |

## Setup

```csharp
// MauiProgram.cs
builder.Services.AddWearables<MyWearableDelegate>();   // manager + delegate
// or
builder.Services.AddWearables();                       // manager only
```

`AddWearables<TDelegate>()` can be called once per delegate; every delegate runs. On targets other than iOS/Android no `IWearableManager` is registered - in shared code resolve it optionally (`services.GetService<IWearableManager>()`) or check `GetStatus().IsSupported`.

### iOS
- The `WCSession` is activated at startup (an `IShinyStartupTask`) so deliveries that arrived while the app was closed reach the delegate. Register during app startup.
- iPad has no WatchConnectivity: `IsSupported` is false. Mac Catalyst/macOS/tvOS have no target.
- iOS has one active watch; its node id is always `watch` and `nodeId` arguments are ignored.

### Android / Wear OS
- Phone and watch apps **must share the application id and signing key**.
- The companion is discovered by the **`shiny_wearable` capability** (`WearableProtocol.Capability`). Shiny advertises it for this app at startup; a Kotlin companion declares it in `res/values/wear.xml` (`android_wear_capabilities`). A .NET Wear OS app using Shiny.Wearables advertises it automatically too.
- Inbound traffic arrives through `ShinyWearableListenerService` (declared by attributes; no manifest edits), bound by Play services for `wear://*/shiny...` - also when the app is not running.
- No Wear OS app / Play services → `IsSupported` is false.
- `IsPaired` and `IsAppInstalled` **survive the watch going away** - a paired watch that is off or out of range is still listed (`IsConnected = false`), found through the capability rather than the connected-node list. The one thing Wear OS cannot see is a paired watch that has **never** had the companion app installed *and* is currently away - the Data Layer exposes nothing that reports it.
- A Wear OS node can be connected **through the cloud** when Bluetooth is out of range: `IsConnected = true`, `IsNearby = false`. Transfers and context still go through; it does not count toward `IsReachable`, because a live message would take seconds.

## Showing watch status - paired vs. connected

Two different questions, and a UI needs both. **Never use one flag for both.**

| Question | Flag | Survives the watch being off / out of range? |
|---|---|---|
| Does this user have a watch set up? (show the "send to watch" feature at all) | `status.IsConfigured` (= `IsPaired && IsAppInstalled`) | **Yes** |
| Is the watch here right now? (a "connected" dot, live messages) | `status.IsReachable` | No |
| Does this platform have a wearable API at all? | `status.IsSupported` | n/a - fixed per device |

**`IsSupported` is not "a watch is set up".** It is false only where there is no wearable API (not iOS/Android, an iPad, Android without Play services). A phone with no watch reports `IsSupported = true`. Gate features on `IsConfigured`.

Gating a feature on `IsReachable` makes it appear and disappear as the user walks around the house. Gating a "connected" indicator on `IsConfigured` claims the watch is receiving while it is charging in another room.

`GetStatus()` is async and there is no event on `IWearableManager` - changes arrive through `IWearableDelegate.OnStatusChanged`, raised on pair/unpair, app installed/removed, and reachability changes. Cache the last status there and bind to that:

```csharp
public class WatchStatusDelegate(WatchStatusState state) : WearableDelegate
{
    public override Task OnStatusChanged(WearableStatus status)
    {
        state.Update(status);   // the delegate runs off the UI thread - marshal inside Update
        return Task.CompletedTask;
    }
}

public class WatchStatusState : INotifyPropertyChanged
{
    public bool HasWatch { get; private set; }      // IsConfigured - stays true while away
    public bool IsConnected { get; private set; }   // IsReachable - live
    public string? WatchName { get; private set; }

    public void Update(WearableStatus status) => MainThread.BeginInvokeOnMainThread(() =>
    {
        this.HasWatch = status.IsConfigured;
        this.IsConnected = status.IsReachable;

        // a paired-but-away watch is still listed, so it can still be named
        this.WatchName = status.Nodes.FirstOrDefault(x => x.HasApp)?.DisplayName;

        this.PropertyChanged?.Invoke(this, new(null));
    });

    public event PropertyChangedEventHandler? PropertyChanged;
}

// register both
services.AddSingleton<WatchStatusState>();
services.AddWearables<WatchStatusDelegate>();

// seed once at startup - OnStatusChanged fires on change, and on Android nothing changes until a capability does
state.Update(await wearables.GetStatus());
```

Per node, `WearableNode` carries `IsConnected` (reachable at all - Bluetooth or cloud), `IsNearby` (direct Bluetooth) and `HasApp`. On iOS `IsConnected` and `IsNearby` are the same flag - WatchConnectivity has no cloud route.

## Code Generation Instructions and Conventions

### Sending

```csharp
public class WatchService(IWearableManager wearables)
{
    public async Task StartWorkout()
    {
        var status = await wearables.GetStatus();
        if (!status.IsSupported || !status.IsAppInstalled)
            return;

        if (status.IsReachable)
        {
            try
            {
                byte[] reply = await wearables.SendMessage("workout/start", Encoding.UTF8.GetBytes("""{"kind":"run"}"""));
            }
            catch (WearableException ex) when (ex.Code == WearableErrorCode.NotReachable)
            {
                // fall back to a queued transfer
                await wearables.Transfer("workout/start", Encoding.UTF8.GetBytes("""{"kind":"run"}"""));
            }
        }

        await wearables.UpdateContext(Encoding.UTF8.GetBytes("""{"plan":"5k"}"""));
        var fileId = await wearables.TransferFile("maps/offline", localPath, new Dictionary<string, string> { ["zoom"] = "14" });

        IReadOnlyList<WearableTransferInfo> pending = await wearables.GetPendingTransfers();
        bool cancelled = await wearables.CancelTransfer(fileId);
    }
}
```

- Paths are app-defined (`sync`, `workout/start`); slashes are trimmed; whitespace, `?`, `#` throw `ArgumentException`.
- Payloads are raw `byte[]` - pick a format (JSON is the usual choice) and use it on both ends.

### Receiving

```csharp
public class MyWearableDelegate : WearableDelegate
{
    // return value = the reply the watch gets; null lets another delegate answer
    public override Task<byte[]?> OnMessageReceived(WearableMessage message)
        => Task.FromResult<byte[]?>(message.Path == "ping" ? Encoding.UTF8.GetBytes("pong") : null);

    public override Task OnContextReceived(WearableContext context) => Task.CompletedTask;
    public override Task OnTransferReceived(WearableTransfer transfer) => Task.CompletedTask;

    // file already moved to {AppData}/Shiny.Wearables/{id}/{name}; the delegate owns it
    public override Task OnFileReceived(WearableFile file) => Task.CompletedTask;

    public override Task OnTransferCompleted(WearableTransferResult result) => Task.CompletedTask; // result.Succeeded / Error
    public override Task OnStatusChanged(WearableStatus status) => Task.CompletedTask;
}
```

- **Reply semantics:** delegates are asked in turn; **the first non-null reply wins**; a throwing delegate is logged and skipped; with no answer the watch gets an empty reply. `message.ExpectsReply` is false when the sender used a fire-and-forget send - the return value is discarded.
- All other callbacks run on every registered delegate.
- On Wear OS, `OnTransferCompleted` fires when the receiver deletes the data item (the delivery receipt), so `result.Path` is empty - track by `Id`.

### Companion apps (wire format - `WearableProtocol`)

**watchOS (Swift, WCSession)**
- Message: `sendMessage(["path": String, "data": Data], replyHandler:)`; reply `["data": Data]`. Messages from the phone always carry a reply handler - always call it.
- Context: `updateApplicationContext(["data": Data])`
- Transfer: `transferUserInfo(["id": String, "path": String, "data": Data])`
- File: `transferFile(url, metadata: ["id": String, "path": String, "name": String, "metadata": [String: String]])`

```swift
WCSession.default.sendMessage(["path": "sync", "data": json],
    replyHandler: { reply in let data = reply["data"] as? Data },
    errorHandler: { print($0) })

func session(_ s: WCSession, didReceiveMessage m: [String: Any], replyHandler: @escaping ([String: Any]) -> Void) {
    replyHandler(["data": handle(m["path"] as? String ?? "", m["data"] as? Data ?? Data())])
}
```

**Wear OS (Kotlin, Data Layer)**
- Message: `MessageClient.sendRequest(nodeId, "/shiny/message/{path}", bytes)`; answer the phone in `WearableListenerService.onRequest` (or `MessageClient.addRpcService`) with `Tasks.forResult(replyBytes)`.
- Context: data item `/shiny/context` with byte array `data` (each node writes its own).
- Transfer: urgent data item `/shiny/transfer/{id}` with `id`, `path`, `data`; the receiver deletes it after handling (that is the sender's receipt).
- File: urgent data item `/shiny/file/{id}` with `id`, `path`, `name`, `metadata` (DataMap of strings), asset `file`; deleted by the receiver.
- Listener service intent filters: `MESSAGE_RECEIVED`, `REQUEST_RECEIVED`, `DATA_CHANGED`; scheme `wear`, host `*`, path prefix `/shiny`.

```kotlin
val node = Wearable.getCapabilityClient(ctx)
    .getCapability("shiny_wearable", CapabilityClient.FILTER_REACHABLE).await().nodes.first()
val reply = Wearable.getMessageClient(ctx).sendRequest(node.id, "/shiny/message/sync", bytes).await()

class PhoneListener : WearableListenerService() {
    override fun onRequest(nodeId: String, path: String, request: ByteArray): Task<ByteArray>? =
        Tasks.forResult(handle(path.removePrefix("/shiny/message/"), request))
}
```

## Namespace Ambiguities

- `WearableStatus`, `WearableMessage`, `WearableContext` etc. are common names - if another library (e.g. `Shiny.AppDeviceBridge.Wearables.Client`) defines contracts with the same names, alias one namespace (`using Native = Shiny.Wearables;`).

## Best Practices

- Check `GetStatus()` before live messages; prefer `Transfer`/`UpdateContext` for anything that must arrive eventually.
- Gate features on `IsConfigured` and a "connected" indicator on `IsReachable` - never `IsSupported` for either.
- Use `UpdateContext` for state, not events - intermediate values are dropped.
- Keep message payloads small; use `TransferFile` for anything large.
- Don't delete a file passed to `TransferFile` until `OnTransferCompleted` reports its id.
- Move or delete files in `OnFileReceived` - they accumulate under `AppData/Shiny.Wearables` otherwise.
- Always return promptly from `OnMessageReceived`: the watch is waiting and WatchConnectivity times out.

## Reference Files

- [API Reference](reference/api-reference.md)
