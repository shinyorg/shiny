---
name: shiny-http-transfers
description: Guide for generating code that uses Shiny.NET HTTP Transfers for background uploads and downloads on iOS/Android, Windows, Linux, macOS, and Blazor WASM (Service Worker Background Sync)
auto_invoke: true
triggers:
  - http transfer
  - background upload
  - background download
  - file upload
  - file download
  - transfer manager
  - HttpTransferManager
  - IHttpTransferManager
  - IHttpTransferDelegate
  - HttpTransferRequest
  - HttpTransferMonitor
  - Shiny.Net.Http
  - azure blob upload
  - aws s3 upload
  - s3 upload
  - AwsS3UploadRequest
  - multipart upload
  - download file
  - upload file
  - transfer progress
---

# Shiny HTTP Transfers

Background HTTP upload and download management. On iOS and Android, backed by native platform transfer APIs (NSURLSession background sessions and WorkManager). On Windows, Linux, macOS, and base .NET, backed by an in-process managed loop using `HttpClient` + `IConnectivity` that wakes on connectivity changes and supports **resumable downloads** via HTTP Range requests (uploads always restart). On Blazor WASM, backed by the Service Worker Background Sync API (IndexedDB queue drained by the SW via `fetch()` while the tab is closed).

## When to Use This Skill

Use this skill when the user needs to:

- Upload or download files in the background on iOS/Android
- Monitor progress of HTTP file transfers
- Queue background transfers that survive app suspension
- Handle transfer errors with automatic retry logic
- Upload files to Azure Blob Storage
- Upload files to AWS S3
- Build a UI that tracks active transfers with progress reporting
- Perform multipart or raw file uploads
- Download files with progress tracking and estimated time remaining

## Library Overview

| Item        | Value                                                                                   |
|-------------|-----------------------------------------------------------------------------------------|
| NuGet       | `Shiny.Net.Http`, `Shiny.Net.Http.Blazor`                                               |
| Namespace   | `Shiny.Net.Http`                                                                        |
| Platforms   | iOS, Android (native); Windows, Linux, macOS, .NET base (managed loop); Blazor WASM (SW) |
| DI Setup    | `services.AddHttpTransfers<TDelegate>()` (iOS/Android/Windows), `services.AddHttpClientTransfers<TDelegate>()` (Linux/macOS/plain .NET), or `services.AddBlazorHttpTransfers<TDelegate>()` (Blazor) |

The registration extension methods live in the `Shiny` namespace and are available on `IServiceCollection`.

## Setup

### 1. Register Services

In your `MauiProgram.cs`:

```csharp
using Shiny;

builder.Services.AddHttpTransfers<MyHttpTransferDelegate>();
```

### 2. Implement the Delegate

Create a class that implements `IHttpTransferDelegate` (or inherits from the abstract `HttpTransferDelegate` base class for built-in retry logic):

```csharp
using Shiny.Net.Http;

public class MyHttpTransferDelegate : HttpTransferDelegate
{
    public MyHttpTransferDelegate(
        ILogger<MyHttpTransferDelegate> logger,
        IHttpTransferManager manager
    ) : base(logger, manager, maxErrorRetries: 3) { }

    public override Task OnCompleted(HttpTransferRequest request)
    {
        // Handle successful transfer completion
        return Task.CompletedTask;
    }

    // Optional: override for 401 handling
    protected override Task<HttpTransferRequest?> OnAuthorizationFailed(
        HttpTransferRequest request, int retries)
    {
        // Return a new request with refreshed auth headers, or null to cancel
        return Task.FromResult<HttpTransferRequest?>(null);
    }
}
```

On Android, the delegate must also implement `IAndroidForegroundServiceDelegate`:

```csharp
#if ANDROID
public partial class MyHttpTransferDelegate : IAndroidForegroundServiceDelegate
{
    public void Configure(AndroidX.Core.App.NotificationCompat.Builder builder)
    {
        builder
            .SetContentTitle("File Transfer")
            .SetContentText("Transferring files in the background");
    }
}
#endif
```

### Linux / macOS / Plain .NET Setup

On non-platform .NET hosts (Linux, macOS server, console apps, etc.) call `AddHttpClientTransfers<TDelegate>()` instead of `AddHttpTransfers<TDelegate>()`. It registers `HttpClientHttpTransferManager` backed by an `HttpClient` loop driven by `IConnectivity` that wakes immediately on connectivity changes. Downloads resume after network interruption via HTTP Range requests (`Range: bytes=N-`, `FileMode.Append` when the server responds with `206 Partial Content`); uploads always restart from scratch.

You must register an `IConnectivity` implementation yourself (e.g. `AddConnectivity()` from `Shiny.Core.Linux` or `Shiny.Core.Blazor`). A default JSON filesystem repository is registered automatically and persists transfer state to `{LocalApplicationData}/Shiny` across process restarts.

Cancelled downloads clean up any partial file on disk so a subsequent re-queue starts fresh.

### Blazor WASM Setup (`Shiny.Net.Http.Blazor`)

```csharp
using Shiny;

builder.Services.AddBlazorHttpTransfers<MyDelegate>(opts =>
{
    opts.ServiceWorkerPath = "./_content/Shiny.Net.Http.Blazor/http-transfer-sw.js";
});
```

The Blazor package uses the Service Worker Background Sync API. Queued transfers are written to IndexedDB; the Service Worker's `sync` event handler drains the queue via `fetch()` and stores download bodies as `Blob`s back into IndexedDB. When the tab reopens, the C# `HttpTransferManager` reconciles results from IndexedDB and fires the `IHttpTransferDelegate` callbacks.

Ship the bundled SW file or import its handlers from your own service worker:

```js
// my-sw.js
importScripts('./_content/Shiny.Net.Http.Blazor/http-transfer-sw.js');
```

**Blazor limitations (v1)**:

- **No resumable downloads** — the SW receives a whole response `Blob`; partial-body appending is not supported.
- **Upload bodies are base64-bridged through JS interop** and persisted as IndexedDB `Blob`s. Fine for small/medium files; very large uploads should wait for a future OPFS streaming path.
- **Browser support for Background Sync is Chromium-only** (no Firefox, no Safari). On unsupported browsers queued transfers drain while the tab is foreground and then sit in IndexedDB until next visit.
- **Retrieving completed downloads**: use `(manager as Shiny.Net.Http.Blazor.HttpTransferManager).GetDownloadBytes(identifier)` which reads the blob back out of IndexedDB as a `byte[]`.

**Do not confuse with `Shiny.Jobs` on Blazor** — Jobs only run while the tab is open because the WASM runtime cannot execute inside a Service Worker. HTTP transfers are the one exception because `fetch()` is pure JS that the SW can run on its own.

## Code Generation Instructions

When generating code that uses Shiny HTTP Transfers, follow these conventions:

### Queuing Transfers

- Always use `IHttpTransferManager` via dependency injection; never instantiate directly.
- `HttpTransferRequest` requires 4 positional parameters: `Identifier`, `Uri`, `TransferType`, `LocalFilePath`:
  ```csharp
  var request = new HttpTransferRequest(
      "my-download",
      "https://example.com/file.zip",
      TransferType.Download,
      Path.Combine(FileSystem.AppDataDirectory, "file.zip")
  );
  await transferManager.Queue(request);
  ```
- Use a unique `Identifier` for each `HttpTransferRequest` so individual transfers can be tracked and cancelled.
- For uploads, ensure the `LocalFilePath` points to an existing file before queuing.
- Set `UseMeteredConnection = false` to restrict large transfers to Wi-Fi only.
- Choose the correct `TransferType`: `UploadMultipart` for form-based uploads, `UploadRaw` for streaming the file body directly, `Download` for downloads.

### Monitoring Progress

- Use `WhenUpdateReceived()` for a global stream of all transfer updates.
- Use the `WatchTransfer(identifier)` extension method to observe a single transfer that completes or errors.
- Use `WatchCount()` to reactively display the number of active transfers.
- For UI binding, use `HttpTransferMonitor` -- call `Start()` to begin monitoring and bind to the `Transfers` collection of `HttpTransferObject` items. These implement `INotifyPropertyChanged`.

### Building Requests

- Use `TransferHttpContent.FromJson(obj)` to attach a JSON body to an upload.
- Use `TransferHttpContent.FromFormData(...)` to attach form-encoded data.
- Use `AzureBlobStorageUploadRequest` for Azure Blob Storage uploads -- call `.WithBlobContainer(tenant, container)` or `.WithCustomUri(uri)`, configure auth via `.WithSasToken()` or `.WithSharedKeyAuthorization()`, then call `.Build()` to get an `HttpTransferRequest`.
- Use `AwsS3UploadRequest` for AWS S3 uploads -- call `.WithBucket(bucket, region)`, configure auth via `.WithPresignedUrl()` or `.WithCredentials(accessKeyId, secretAccessKey)`, optionally set `.WithObjectKey()`, `.WithContentType()`, `.WithStorageClass()`, then call `.Build()` to get an `HttpTransferRequest`. Uses AWS Signature V4 signing with `UNSIGNED-PAYLOAD` -- no AWS SDK required.
- Use `AppleHttpTransferRequest` (inherits `HttpTransferRequest`) when Apple-specific options are needed (e.g., `AllowsConstrainedNetworkAccess`, `AllowsCellularAccess`, `AssumesHttp3Capable`).

### Foreground (Non-Background) Transfers

- For transfers that only need to run while the app is in the foreground, use the `HttpClient` extension methods `Upload(...)` and `Download(...)` which return `IObservable<TransferProgress>` with real-time progress.

### Platform Configuration (Apple)

- Optionally register an `INativeConfigurator` implementation to customize `NSUrlSessionConfiguration` and `NSMutableUrlRequest` objects before they are sent.

## Best Practices

1. **Use the abstract base class** -- `HttpTransferDelegate` provides built-in retry and 401-handling logic. Only implement `IHttpTransferDelegate` directly if you need full control.
2. **Validate before queuing** -- Call `request.AssertValid()` to check the request is well-formed before calling `Queue()`.
3. **Observe on the main thread** -- When binding `HttpTransferMonitor` to UI, pass a `scheduler` (e.g., `RxApp.MainThreadScheduler` or `SynchronizationContext.Current`) to `Start()`.
4. **Clean up the monitor** -- `HttpTransferMonitor` implements `IDisposable`. Dispose it when the page or view model is torn down.
5. **Handle metered connections** -- Set `UseMeteredConnection = false` for large files so the system waits for an unmetered (Wi-Fi) connection.
6. **Unique identifiers** -- Always provide meaningful, unique identifiers for transfers so they can be individually tracked, cancelled, and retried.

## Reference Files

- [API Reference](reference/api-reference.md)
