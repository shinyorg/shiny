using System.IO;
using Microsoft.Maui.Storage;
using Shiny.Net;
using Shiny.Net.Http;
using Shiny.Tests.Mocks;
#if ANDROID
using AndroidX.Core.App;
#endif

namespace Shiny.Tests;


public class HttpTransferTests : AbstractShinyTests
{
    // TODO: test multiple uploads while monitoring
    // TODO: mock disconnects

    // point at our test api
    public HttpTransferTests(ITestOutputHelper output) : base(output) { }


    public override void Dispose()
    {
        var transfers = Directory.GetFiles(FileSystem.AppDataDirectory, "*.transfer");
        foreach (var transfer in transfers)
        {
            try
            {
                File.Delete(transfer);
            }
            catch {}
        }
        base.Dispose();
    }


    protected override void Configure(HostBuilder hostBuilder)
    {
#if ANDROID
        hostBuilder.Services.AddSingleton<IConnectivity, MockConnectivity>();
#endif
        hostBuilder.Services.AddHttpTransfers<TestHttpTransferDelegate>();
    }


    [Theory(DisplayName = "HTTP Transfers - Cancel")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancel(bool isUpload)
    {
        var manager = this.GetService<IHttpTransferManager>();

        var id = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource();

        using var sub = manager
            .WhenUpdateReceived()
            .Subscribe(x =>
            {
                this.Log($"[{x.Request.Identifier}]({x.Status}): {x.Progress.PercentComplete * 100}% - {x.Progress.BytesPerSecond} b/s");
                if (x.Exception != null)
                    this.Log("ERROR: " + x.Exception);
            });

        var startedTask = manager
            .WhenUpdateReceived()
            .Where(x => x.Status == HttpTransferState.InProgress)
            .Take(1)
            .ToTask();

        var cancelTask = manager
            .WhenUpdateReceived()
            .Where(x => x.Status == HttpTransferState.Canceled)
            .Take(1)
            .ToTask();

        var transfer = await manager.Queue(new(
            id,
            this.GetUri(isUpload, false),
            isUpload,
            this.GetLocalPath(isUpload)
        ));

        this.Log("Waiting for transfer to start");
        await startedTask.ConfigureAwait(false);
#if ANDROID
        HttpTransferService.IsStarted.Should().Be(true, "Android foreground service should have started");
#endif

        await manager.CancelAll();
        this.Log("Waiting for transfer to cancel");

        await cancelTask.ConfigureAwait(false);

#if ANDROID
        HttpTransferService.IsStarted.Should().Be(false, "Android foreground service should have stopped");
#endif
    }


    [Theory(DisplayName = "HTTP Transfers - Observable - Error Handling")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ErrorHandlingObservable(bool isUpload)
    {
        var manager = this.GetService<IHttpTransferManager>();

        var id = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource();

        var errorTask = manager
            .WhenUpdateReceived()
            .Where(x => x.Status == HttpTransferState.Error)
            .Take(1)
            .Timeout(TimeSpan.FromSeconds(10))
            .ToTask();

        var transfer = await manager.Queue(new(
            id,
            this.TestUri + "/transfers/error",
            isUpload,
            this.GetLocalPath(isUpload)
        ));

        await errorTask.ConfigureAwait(false);

#if ANDROID
        HttpTransferService.IsStarted.Should().Be(false, "Android foreground service should have stopped");
#endif
    }


    [Theory(DisplayName = "HTTP Transfers - Delegate - Error Handling")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ErrorHandlingDelegate(bool isUpload)
    {
        var manager = this.GetService<IHttpTransferManager>();
        var id = Guid.NewGuid().ToString();
        var tdelegate = this.GetService<TestHttpTransferDelegate>();
        var tcs = new TaskCompletionSource<Exception?>();

        tdelegate.OnFinish = args => tcs.TrySetResult(args.Exception);

        var transfer = await manager.Queue(new(
            id,
            this.TestUri + "/transfers/error",
            isUpload,
            this.GetLocalPath(isUpload)
        ));

        var exception = await tcs.Task
            .WaitAsync(TimeSpan.FromSeconds(10))
            .ConfigureAwait(false);

        exception.Should().NotBeNull("Exception should be set");

#if ANDROID
        HttpTransferService.IsStarted.Should().Be(false, "Android foreground service should have stopped");
#endif
    }


    [Fact(DisplayName = "HTTP Transfers - Upload with Form Data")]
    public async Task UploadWithFormPost()
    {
        var manager = this.GetService<IHttpTransferManager>();
        var id = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource();

        using var sub = manager.WatchTransfer(id).Subscribe(
            x => this.Log($"{x.Progress.PercentComplete * 100}% complete - bp/s: {x.Progress.BytesPerSecond}"),
            ex => tcs.TrySetException(ex),
            () => tcs.TrySetResult()
        );

        await manager.Queue(new HttpTransferRequest(
            id,
            this.GetUri(true, true),
            true,
            this.GetLocalPath(true),
            HttpContent: TransferHttpContent.FromFormData(
                ("Text", "This is a test")
            )
        ));
        await tcs.Task.ConfigureAwait(false);
    }


    [Theory(DisplayName = "HTTP Transfers - Delegate")]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task TestDelegate(bool isUpload, bool includeBody)
    {
        var manager = this.GetService<IHttpTransferManager>();
        var id = Guid.NewGuid().ToString();
        var tdelegate = this.GetService<TestHttpTransferDelegate>();
        var tcs = new TaskCompletionSource();

        using var sub = manager.WatchTransfer(id).Subscribe(
            x => this.Log($"{x.Progress.PercentComplete * 100}% complete - b/s: {x.Progress.BytesPerSecond}"),
            _ => { }
        );

        tdelegate.OnFinish = args =>
        {
            if (args.Exception == null)
                tcs.TrySetResult();
            else
                tcs.TrySetException(args.Exception);
        };

        TransferHttpContent? content = null;
        if (includeBody)
            content = TransferHttpContent.FromJson(new { Text = "This is test JSON" });

        await manager.Queue(new(
            id,
            this.GetUri(isUpload, includeBody),
            isUpload,
            this.GetLocalPath(isUpload),
            Headers: new Dictionary<string, string>
            {
                { "Test", "Test" }
            },
            HttpContent: content
        )
        {
            HttpMethod = includeBody ? "POST" : "GET"
        });
        await tcs.Task.ConfigureAwait(false);

#if ANDROID
        HttpTransferService.IsStarted.Should().Be(false, "Android foreground service should have stopped");
#endif
    }


    [Theory(DisplayName = "HTTP Transfers - Observable")]
    [InlineData(true)]
    [InlineData(false)]
    public async void TestObservable(bool isUpload)
    {
        var manager = this.GetService<IHttpTransferManager>();

        var id = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource();
        
        using var sub = manager.WatchTransfer(id).Subscribe(
            x => this.Log($"{x.Progress.PercentComplete * 100}% complete - bp/s: {x.Progress.BytesPerSecond}"),
            ex => tcs.TrySetException(ex),
            () => tcs.TrySetResult()
        );

        await manager.Queue(new HttpTransferRequest(
            id,
            this.GetUri(isUpload, false),
            isUpload,
            this.GetLocalPath(isUpload),
            Headers: new Dictionary<string, string>
            {
                { "Test", "Test" }
            }
        ));
        await tcs.Task.ConfigureAwait(false);
    }


#if ANDROID
    // Test connectivity flops by injecting mock connectivity
    //[Fact(DisplayName = "HTTP Transfers (Android) - Connectivity Test")]
    //public async Task AndroidConnectivityTest()
    //{
    //    var conn = this.GetService<MockConnectivity>();
    //    conn.Change(NetworkAccess.None);

    //    var manager = this.GetService<IHttpTransferManager>();
    //    var id = Guid.NewGuid().ToString();

    //    //await manager.Queue(new HttpTransferRequest(
    //    //    id,
    //    //    this.GetUri(isUpload, false),
    //    //    isUpload,
    //    //    this.GetLocalPath(isUpload),
    //    //    Headers: new Dictionary<string, string>
    //    //    {
    //    //        { "Test", "Test" }
    //    //    }
    //    //));
    //    // TODO: ensure fg service stays on
    //}
#endif

    string GetUri(bool upload, bool includeBody)
    {
        var uri = this.TestUri + "/transfers";
        uri += upload ? "/upload" : "/download";
        if (includeBody)
            uri += "/body";

        this.Log($"Upload: {upload} - URI: {uri}");
        return uri;
    }


    const int UPLOAD_SIZE_MB = 50;
    string GetLocalPath(bool isUpload)
    {
        string path = null!;

        if (isUpload)
        {
            path = Path.Combine(FileSystem.AppDataDirectory, "upload.bin");
            if (!File.Exists(path))
            {
                this.Log("Generating Upload Binary");
                Utils.GenerateFile(path, UPLOAD_SIZE_MB);
                this.Log($"Upload File Generated - {new FileInfo(path).Length} bytes");
            }
        }
        else
        {
            var fn = $"{Guid.NewGuid()}.transfer";
            path = Path.Combine(FileSystem.AppDataDirectory, fn);
        }
        return path;
    }


    public string TestUri
    {
        get
        {
            var cfg = MauiProgram.Configuration;
            var uri = cfg["HttpTestsUrl"];
            if (String.IsNullOrWhiteSpace(uri))
                throw new InvalidProgramException("HttpTestsUrl is not set in appsettings.json");

            return uri;
        }
    }
}


public partial class TestHttpTransferDelegate : IHttpTransferDelegate
{
    public Action<(HttpTransferRequest Request, Exception? Exception)>? OnFinish { get; set; }


    public Task OnCompleted(HttpTransferRequest request)
    {
        this.OnFinish?.Invoke((request, null));
        return Task.CompletedTask;
    }


    public Task OnError(HttpTransferRequest request, Exception ex)
    {
        this.OnFinish?.Invoke((request, ex));
        return Task.CompletedTask;
    }
}

#if ANDROID
public partial class TestHttpTransferDelegate : IAndroidForegroundServiceDelegate
{
    public void Configure(NotificationCompat.Builder builder)
    {
        builder
            .SetContentTitle("Shiny Integration Tests")
            .SetContentText("Running HTTP Transfer Test");
    }
}

#endif