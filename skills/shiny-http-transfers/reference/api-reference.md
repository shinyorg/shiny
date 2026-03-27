# Shiny HTTP Transfers - API Reference

## Installation

```xml
<PackageReference Include="Shiny.Net.Http" Version="4.0.0" />
```

## Namespace

```csharp
using Shiny.Net.Http;
```

The DI registration extension method is in the `Shiny` namespace:

```csharp
using Shiny;
```

---

## DI Registration

```csharp
public static class ServiceCollectionExtensions
{
    // Registers IHttpTransferManager, the delegate, HttpTransferMonitor, and all platform infrastructure
    public static IServiceCollection AddHttpTransfers<TDelegate>(this IServiceCollection services)
        where TDelegate : class, IHttpTransferDelegate;
}
```

---

## Interfaces

### IHttpTransferManager

The primary service for queuing, tracking, and cancelling background HTTP transfers.

```csharp
public interface IHttpTransferManager
{
    // Gets all pending and active transfers
    Task<IList<HttpTransfer>> GetTransfers();

    // Queues a new HTTP transfer for background execution
    Task<HttpTransfer> Queue(HttpTransferRequest request);

    // Cancels a transfer by identifier
    Task Cancel(string identifier);

    // Cancels all pending and active transfers
    Task CancelAll();

    // Returns an observable that emits the current transfer count when it changes
    IObservable<int> WatchCount();

    // Returns an observable that emits transfer progress and completion updates
    IObservable<HttpTransferResult> WhenUpdateReceived();
}
```

### IHttpTransferDelegate

Delegate that receives HTTP transfer lifecycle events. Register your implementation via `AddHttpTransfers<TDelegate>()`.

```csharp
public interface IHttpTransferDelegate
{
    // Called when a transfer encounters an error
    // statusCode is the HTTP status code, or 0 for non-HTTP errors
    Task OnError(HttpTransferRequest request, int statusCode, Exception ex);

    // Called when a transfer completes successfully
    Task OnCompleted(HttpTransferRequest request);
}
```

### INativeConfigurator (Apple/iOS only)

Optional interface to customize native Apple NSUrlSession configuration and request objects. Register in DI to use.

```csharp
// Platform: iOS only
// Namespace: Shiny.Net.Http
public interface INativeConfigurator
{
    // Configures the NSUrlSession configuration used for background transfers
    void Configure(NSUrlSessionConfiguration configuration);

    // Configures a native URL request before it is sent
    void Configure(NSMutableUrlRequest nativeRequest, HttpTransferRequest request);
}
```

---

## Enums

### TransferType

```csharp
public enum TransferType
{
    UploadMultipart,  // Upload as multipart/form-data
    UploadRaw,        // Upload the file body directly (raw stream)
    Download           // Download a file
}
```

### HttpTransferState

```csharp
public enum HttpTransferState
{
    Unknown,
    Pending,
    Paused,
    PausedByNoNetwork,
    PausedByCostedNetwork,
    InProgress,
    Error,
    Canceled,
    Completed
}
```

---

## Records and Models

### HttpTransferRequest

The configuration for a background HTTP transfer.

```csharp
public record HttpTransferRequest(
    string Identifier,                          // Unique identifier for the transfer
    string Uri,                                 // The remote URI to upload to or download from
    TransferType Type,                          // Upload or download
    string LocalFilePath,                       // Local file path (source for uploads, destination for downloads)
    bool UseMeteredConnection = true,           // Whether to allow metered/cellular connections
    TransferHttpContent? HttpContent = null,    // Optional body content for uploads
    IDictionary<string, string>? Headers = null // Optional HTTP headers
)
{
    // Override the HTTP method (defaults to GET for downloads, POST for uploads)
    public string? HttpMethod { get; set; }

    // The form data field name for the file in multipart uploads (default: "file")
    public string FileFormDataName { get; set; } = "file";

    // Returns the resolved HttpMethod based on Type and HttpMethod property
    public HttpMethod GetHttpMethod();
}
```

### HttpTransfer

Represents a queued or active transfer with its current state.

```csharp
public record HttpTransfer(
    HttpTransferRequest Request,
    long? BytesToTransfer,
    long BytesTransferred,
    HttpTransferState Status,
    DateTimeOffset CreatedAt
) : IRepositoryEntity
{
    public string Identifier => this.Request.Identifier;
}
```

### HttpTransferResult

Emitted by `WhenUpdateReceived()` and `WatchTransfer()` to report transfer progress.

```csharp
public record HttpTransferResult(
    HttpTransferRequest Request,
    HttpTransferState Status,
    TransferProgress Progress,
    Exception? Exception
)
{
    // True if total bytes to transfer is known
    public bool IsDeterministic => this.Progress.BytesToTransfer != null;
}
```

### TransferProgress

Progress metrics for a transfer.

```csharp
public record TransferProgress(
    long BytesPerSecond,        // Current transfer speed
    long? BytesToTransfer,      // Total bytes (null if indeterminate)
    long BytesTransferred       // Bytes transferred so far
)
{
    public static TransferProgress Empty { get; }

    // True if total bytes to transfer is known
    public bool IsDeterministic => this.BytesToTransfer != null;

    // Percentage complete (0.0 to 1.0), or -1 if indeterminate
    public double PercentComplete { get; }

    // Estimated time remaining, or TimeSpan.Zero if indeterminate
    public TimeSpan EstimatedTimeRemaining { get; }
}
```

### TransferHttpContent

Describes body content to send with upload requests.

```csharp
public record TransferHttpContent(
    string Content,                          // The string content
    string ContentType = "text/plain",       // MIME content type
    string Encoding = "utf-8"                // Character encoding (web name)
)
{
    // Form data field name for the content in multipart uploads
    public string? ContentFormDataName { get; set; }

    // Create from a serializable object as JSON
    public static TransferHttpContent FromJson(
        object obj,
        JsonSerializerOptions? jsonOptions = null
    );

    // Create from key-value tuples as application/x-www-form-urlencoded
    public static TransferHttpContent FromFormData(
        params (string Key, string Value)[] formValues
    );

    // Create from a dictionary as application/x-www-form-urlencoded
    public static TransferHttpContent FromFormData(
        IDictionary<string, string> dictionary
    );
}
```

### AppleHttpTransferRequest (iOS only)

Extends `HttpTransferRequest` with Apple-specific network configuration options.

```csharp
// Platform: iOS only
public record AppleHttpTransferRequest(
    string Identifier,
    string Uri,
    TransferType Type,
    string LocalFilePath,
    bool UseMeteredConnection = true,
    TransferHttpContent? BodyContent = null,
    IDictionary<string, string>? Headers = null,
    bool AllowsConstrainedNetworkAccess = true,  // Allow low-data mode connections
    bool AllowsCellularAccess = true,             // Allow cellular connections
    bool? AssumesHttp3Capable = null              // Hint that server supports HTTP/3 (iOS 14.5+)
) : HttpTransferRequest(
    Identifier, Uri, Type, LocalFilePath,
    UseMeteredConnection, BodyContent, Headers
);
```

---

## Abstract Base Class

### HttpTransferDelegate

Abstract base class implementing `IHttpTransferDelegate` with built-in error retry and 401 authorization retry logic. On Android, also implements `IAndroidForegroundServiceDelegate`.

```csharp
public abstract partial class HttpTransferDelegate(
    ILogger logger,
    IHttpTransferManager manager,
    int maxErrorRetries = 0
) : IHttpTransferDelegate
{
    protected ILogger Logger { get; }
    protected IHttpTransferManager TransferManager { get; }

    // Maximum number of error retry attempts (set via constructor or property)
    public int MaxErrorRetryAttempts { get; set; }

    // Called on HTTP 401 -- return a new request with refreshed auth headers, or null to cancel
    protected virtual Task<HttpTransferRequest?> OnAuthorizationFailed(
        HttpTransferRequest request, int retries
    );

    // Called before an error retry -- mutate the request or return null to cancel
    protected virtual Task<HttpTransferRequest?> OnBeforeRetry(
        HttpTransferRequest request, int retries
    );

    // Handles error routing (auth vs general errors) with automatic retry
    public Task OnError(HttpTransferRequest request, int statusCode, Exception ex);

    // Override to handle successful completion (default: no-op)
    public virtual Task OnCompleted(HttpTransferRequest request);
}

// Android partial:
// public abstract partial class HttpTransferDelegate : IAndroidForegroundServiceDelegate
// {
//     public abstract void Configure(AndroidX.Core.App.NotificationCompat.Builder builder);
// }
```

---

## UI Monitoring

### HttpTransferMonitor

A service for monitoring transfers in the UI with observable collection support. Registered automatically by `AddHttpTransfers<T>()`.

```csharp
public class HttpTransferMonitor(
    IHttpTransferManager manager,
    IRepository repository,
    ILogger<HttpTransferMonitor> logger
) : IDisposable
{
    // Observable collection of transfer view objects
    public INotifyReadOnlyCollection<HttpTransferObject> Transfers { get; }

    // Whether the monitor is currently active
    public bool IsStarted { get; }

    // Start monitoring transfers. Pass a scheduler for UI thread marshalling.
    public Task Start(
        bool removeFinished = true,
        bool removeErrors = true,
        bool removeCancelled = true,
        IScheduler? scheduler = null
    );

    // Remove items from the Transfers collection by state
    public void Clear(bool removeFinished, bool removeCancelled, bool removeErrors);

    // Stop monitoring
    public void Stop();

    // Calls Stop()
    public void Dispose();
}
```

### HttpTransferObject

A bindable view object representing a single transfer. Implements `INotifyPropertyChanged`.

```csharp
public class HttpTransferObject : NotifyPropertyChanged
{
    public HttpTransferRequest Request { get; }
    public string Identifier { get; }
    public string Uri { get; }
    public TransferType Type { get; }

    // All properties below raise PropertyChanged
    public double PercentComplete { get; }           // 0.0 to 1.0, or -1 if indeterminate
    public long BytesPerSecond { get; }
    public bool IsDeterministic { get; }
    public long? BytesToTransfer { get; }
    public long BytesTransferred { get; }
    public TimeSpan EstimatedTimeRemaining { get; }
    public HttpTransferState Status { get; }
}
```

---

## Azure Blob Storage Helper

### AzureBlobStorageUploadRequest

Fluent builder for constructing `HttpTransferRequest` objects targeting Azure Blob Storage.

```csharp
public class AzureBlobStorageUploadRequest(string localFilePath)
{
    public string LocalFilePath { get; }
    public string? Identifier { get; set; }
    public string? Uri { get; set; }
    public bool UseMeteredConnection { get; set; }
    public string? SharedAuthorizationKey { get; set; }
    public DateTimeOffset? AuthVersion { get; set; }
    public DateTimeOffset? AuthDate { get; set; }
    public string? SasToken { get; set; }
    public Dictionary<string, string> Headers { get; }

    // Set URI to https://{tenant}.blob.core.windows.net/{containerName}
    public AzureBlobStorageUploadRequest WithBlobContainer(string tenant, string containerName);

    // Set a custom URI
    public AzureBlobStorageUploadRequest WithCustomUri(string uri);

    // Allow metered connections
    public AzureBlobStorageUploadRequest WithMeteredConnection();

    // Add a custom header
    public AzureBlobStorageUploadRequest WithHeader(string key, string value);

    // Authenticate via SAS token (appended as query string)
    public AzureBlobStorageUploadRequest WithSasToken(string sasToken);

    // Authenticate via shared key authorization header
    public AzureBlobStorageUploadRequest WithSharedKeyAuthorization(
        string sharedKey,
        DateTimeOffset? Version = null,
        DateTimeOffset? AuthDate = null
    );

    // Build the final HttpTransferRequest (uses PUT, UploadRaw, sets x-ms-blob-type header)
    public HttpTransferRequest Build();
}
```

---

## Extension Methods

### HttpTransferExtensions

```csharp
public static class HttpTransferExtensions
{
    // Validates an HttpTransferRequest: checks Identifier is set, file exists for uploads
    // Throws InvalidOperationException or ArgumentException on failure
    public static void AssertValid(this HttpTransferRequest request);

    // Returns true if the TransferType is an upload (UploadMultipart or UploadRaw)
    public static bool IsUpload(this TransferType type);

    // Monitors a specific transfer by identifier.
    // Completes when the transfer finishes. Errors if the transfer fails.
    // Unlike WhenUpdateReceived(), this observable terminates.
    public static IObservable<HttpTransferResult> WatchTransfer(
        this IHttpTransferManager manager,
        string identifier
    );
}
```

### HttpClientExtensions

Foreground-only transfer helpers on `HttpClient` that return `IObservable<TransferProgress>` with real-time progress. These do NOT use the background transfer system.

```csharp
public static class HttpClientExtensions
{
    // Upload a file with progress reporting
    public static IObservable<TransferProgress> Upload(
        this HttpClient httpClient,
        string uri,
        string filePath,
        bool sendAsMultipart,
        HttpMethod? httpMethod = null,
        HttpContent? bodyContent = null,
        string contentFormDataName = "value",
        string fileFormDataName = "file",
        params (string Name, string Value)[] headers
    );

    // Download a file with progress reporting
    public static IObservable<TransferProgress> Download(
        this HttpClient httpClient,
        string uri,
        string toFilePath,
        int bufferSize = 8192,
        HttpMethod? httpMethod = null,
        HttpContent? bodyContent = null,
        params (string Name, string Value)[] headers
    );
}
```

---

## Android Notification Strategies

### AbstractTransferNotificationStrategy

Base class for Android notification display during background transfers. Implements `IShinyStartupTask`.

```csharp
// Platform: Android only
public abstract class AbstractTransferNotificationStrategy : IShinyStartupTask
{
    public abstract void Start();
    protected string NotificationChannelId { get; set; } // Default: "Transfers"
    protected virtual NotificationCompat.Builder CreateBuilder(string channelId);
    protected virtual string CreateChannel();
}
```

### PerTransferNotificationStrategy

Displays one notification per active transfer with progress.

```csharp
// Platform: Android only
public class PerTransferNotificationStrategy : AbstractTransferNotificationStrategy
{
    public PerTransferNotificationStrategy(
        IHttpTransferManager manager,
        AndroidPlatform platform,
        ILogger<PerTransferNotificationStrategy> logger
    );

    public override void Start();
    protected virtual void Customize(NotificationCompat.Builder builder, HttpTransferResult result);
}
```

---

## Usage Examples

### Queue a Download

```csharp
public class MyViewModel
{
    readonly IHttpTransferManager transferManager;

    public MyViewModel(IHttpTransferManager transferManager)
    {
        this.transferManager = transferManager;
    }

    public async Task DownloadFile()
    {
        var request = new HttpTransferRequest(
            Identifier: "download-report-123",
            Uri: "https://example.com/files/report.pdf",
            Type: TransferType.Download,
            LocalFilePath: Path.Combine(
                FileSystem.AppDataDirectory, "report.pdf"
            ),
            UseMeteredConnection: false
        );

        request.AssertValid();
        var transfer = await this.transferManager.Queue(request);
    }
}
```

### Queue a Multipart Upload with JSON Body

```csharp
var content = TransferHttpContent.FromJson(new { userId = 42, category = "documents" });

var request = new HttpTransferRequest(
    Identifier: "upload-doc-456",
    Uri: "https://api.example.com/upload",
    Type: TransferType.UploadMultipart,
    LocalFilePath: "/path/to/document.pdf",
    HttpContent: content,
    Headers: new Dictionary<string, string>
    {
        ["Authorization"] = "Bearer my-token"
    }
);

await transferManager.Queue(request);
```

### Queue a Raw Upload

```csharp
var request = new HttpTransferRequest(
    Identifier: "raw-upload-789",
    Uri: "https://api.example.com/files",
    Type: TransferType.UploadRaw,
    LocalFilePath: "/path/to/data.bin"
)
{
    HttpMethod = "PUT"
};

await transferManager.Queue(request);
```

### Watch a Single Transfer

```csharp
transferManager
    .WatchTransfer("download-report-123")
    .Subscribe(
        result =>
        {
            Console.WriteLine($"Progress: {result.Progress.PercentComplete:P0}");
            Console.WriteLine($"Speed: {result.Progress.BytesPerSecond} B/s");
            Console.WriteLine($"ETA: {result.Progress.EstimatedTimeRemaining}");
        },
        ex => Console.WriteLine($"Transfer failed: {ex.Message}"),
        () => Console.WriteLine("Transfer completed!")
    );
```

### Monitor All Transfers in the UI

```csharp
public class TransferListViewModel : IDisposable
{
    readonly HttpTransferMonitor monitor;

    public TransferListViewModel(HttpTransferMonitor monitor)
    {
        this.monitor = monitor;
    }

    public INotifyReadOnlyCollection<HttpTransferObject> Transfers
        => this.monitor.Transfers;

    public async Task OnAppearing()
    {
        // Pass a scheduler if using MVVM framework for UI thread marshalling
        await this.monitor.Start(
            removeFinished: true,
            removeErrors: false,
            removeCancelled: true
        );
    }

    public void Dispose() => this.monitor.Dispose();
}
```

### Upload to Azure Blob Storage

```csharp
var request = new AzureBlobStorageUploadRequest("/path/to/file.zip")
    .WithBlobContainer("myaccount", "my-container")
    .WithSasToken("sv=2021-06-08&ss=b&srt=o&sp=w&se=...")
    .Build();

await transferManager.Queue(request);
```

### Upload to Azure Blob Storage with Shared Key

```csharp
var request = new AzureBlobStorageUploadRequest("/path/to/file.zip")
    .WithBlobContainer("myaccount", "my-container")
    .WithSharedKeyAuthorization("mySharedKey")
    .Build();

await transferManager.Queue(request);
```

### Foreground Download with HttpClient

```csharp
using var httpClient = new HttpClient();

httpClient
    .Download(
        "https://example.com/largefile.zip",
        "/local/path/largefile.zip",
        bufferSize: 8192
    )
    .Subscribe(
        progress =>
        {
            Console.WriteLine($"{progress.PercentComplete:P0} - {progress.BytesPerSecond} B/s");
        },
        ex => Console.WriteLine($"Error: {ex.Message}"),
        () => Console.WriteLine("Download complete")
    );
```

### Foreground Upload with HttpClient

```csharp
using var httpClient = new HttpClient();

httpClient
    .Upload(
        "https://api.example.com/upload",
        "/local/path/photo.jpg",
        sendAsMultipart: true,
        fileFormDataName: "file",
        headers: ("Authorization", "Bearer token123")
    )
    .Subscribe(
        progress =>
        {
            Console.WriteLine($"{progress.PercentComplete:P0}");
        },
        ex => Console.WriteLine($"Error: {ex.Message}"),
        () => Console.WriteLine("Upload complete")
    );
```

### Apple-Specific Request Configuration

```csharp
#if IOS
var request = new AppleHttpTransferRequest(
    Identifier: "ios-download-001",
    Uri: "https://example.com/large-file.zip",
    Type: TransferType.Download,
    LocalFilePath: Path.Combine(FileSystem.AppDataDirectory, "large-file.zip"),
    AllowsConstrainedNetworkAccess: false,
    AllowsCellularAccess: false,
    AssumesHttp3Capable: true
);

await transferManager.Queue(request);
#endif
```

### Implementing INativeConfigurator (iOS)

```csharp
#if IOS
using Foundation;
using Shiny.Net.Http;

public class MyNativeConfigurator : INativeConfigurator
{
    public void Configure(NSUrlSessionConfiguration configuration)
    {
        configuration.TimeoutIntervalForResource = 3600; // 1 hour
        configuration.HttpMaximumConnectionsPerHost = 2;
    }

    public void Configure(NSMutableUrlRequest nativeRequest, HttpTransferRequest request)
    {
        nativeRequest.TimeoutInterval = 120;
    }
}
#endif
```

### Delegate with Auth Retry

```csharp
public class AuthRetryDelegate : HttpTransferDelegate
{
    readonly IAuthService authService;

    public AuthRetryDelegate(
        ILogger<AuthRetryDelegate> logger,
        IHttpTransferManager manager,
        IAuthService authService
    ) : base(logger, manager, maxErrorRetries: 3)
    {
        this.authService = authService;
    }

    protected override async Task<HttpTransferRequest?> OnAuthorizationFailed(
        HttpTransferRequest request, int retries)
    {
        if (retries >= 2)
            return null; // Give up after 2 auth retries

        var newToken = await this.authService.RefreshTokenAsync();
        var headers = new Dictionary<string, string>(request.Headers ?? new Dictionary<string, string>())
        {
            ["Authorization"] = $"Bearer {newToken}"
        };

        return request with { Headers = headers };
    }

    public override Task OnCompleted(HttpTransferRequest request)
    {
        this.Logger.LogInformation("Transfer {Id} completed successfully", request.Identifier);
        return Task.CompletedTask;
    }
}
```

---

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| `InvalidOperationException: Identifier is not set` | `HttpTransferRequest.Identifier` is empty or null | Provide a non-empty unique identifier |
| `ArgumentException: ... does not exist` | Upload file path does not exist on disk | Ensure the file exists before calling `Queue()` |
| Transfer pauses on cellular | `UseMeteredConnection` is `false` | Set `UseMeteredConnection = true` or wait for Wi-Fi |
| No progress events received | Not subscribed to `WhenUpdateReceived()` or `WatchTransfer()` | Subscribe to the observable and keep the subscription alive |
| `InvalidOperationException: Already running monitor` | `HttpTransferMonitor.Start()` called twice | Call `Stop()` before calling `Start()` again |
| Transfer stuck in `Pending` on Android | No internet or foreground service not configured | Ensure the delegate implements `IAndroidForegroundServiceDelegate` and internet is available |
| 401 errors not retrying | Using `IHttpTransferDelegate` directly instead of `HttpTransferDelegate` base class | Use `HttpTransferDelegate` and override `OnAuthorizationFailed` |
| iOS transfers not resuming after app kill | Not using `NSUrlSession` background configuration properly | Ensure `INativeConfigurator` is registered if custom session config is needed |
