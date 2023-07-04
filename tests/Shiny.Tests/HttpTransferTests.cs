using System.IO;
using Microsoft.Maui.Storage;
using Shiny.Net.Http;

namespace Shiny.Tests;


public class HttpTransferTests : AbstractShinyTests
{
    const string TEST_DOWNLOAD_URI = "http://ipv4.download.thinkbroadband.com/5MB.zip";
    //const string TEST_DOWNLOAD_URI = "http://ipv4.download.thinkbroadband.com/50MB.zip";
    //const string TEST_DOWNLOAD_URI = "http://ipv4.download.thinkbroadband.com/512MB.zip";

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
        hostBuilder.Services.AddHttpTransfers<TestHttpTransferDelegate>();
    }


    [Theory(DisplayName = "HTTP Transfers - Cancel")]
    [InlineData(TEST_DOWNLOAD_URI, false)]
    //[InlineData("", true)]
    public async void Cancel(string uri, bool isUpload)
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

        // TODO: upload will need an actual file
        var transfer = await manager.Queue(new HttpTransferRequest(
            id,
            uri,
            isUpload,
            $"./{id}.transfer"
        ));

        this.Log("Waiting for transfer to start");
        await startedTask.ConfigureAwait(false);

        await manager.CancelAll();
        this.Log("Waiting for transfer to cancel");

        await cancelTask.ConfigureAwait(false);
    }


    [Theory(DisplayName = "HTTP Transfers - Delegate")]
    [InlineData(TEST_DOWNLOAD_URI, false)]
    //[InlineData("", true)]
    public async Task TestDelegate(string uri, bool isUpload)
    {
        var manager = this.GetService<IHttpTransferManager>();
        var tdelegate = this.GetService<TestHttpTransferDelegate>();
        var id = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource();

        using var sub = manager.WatchTransfer(id).Subscribe(
            x => this.Log($"{x.Progress.PercentComplete * 100}% complete - b/s: {x.Progress.BytesPerSecond}")
        );

        tdelegate.OnFinish = args =>
        {
            if (args.Exception == null)
                tcs.TrySetResult();
            else
                tcs.TrySetException(args.Exception);
        };

        // TODO: upload will need an actual file
        await manager.Queue(new HttpTransferRequest(
            id,
            uri,
            isUpload,
            $"{FileSystem.AppDataDirectory}/{id}.transfer"
        ));
        await tcs.Task.ConfigureAwait(false);
    }


    [Theory(DisplayName = "HTTP Transfers - Observable")]
    [InlineData(TEST_DOWNLOAD_URI, false)]
    //[InlineData("", true)]
    public async void TestObservable(string uri, bool isUpload)
    {
        var manager = this.GetService<IHttpTransferManager>();

        var id = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource();
        
        using var sub = manager.WatchTransfer(id).Subscribe(
            x => this.Log($"{x.Progress.PercentComplete * 100}% complete - bp/s: {x.Progress.BytesPerSecond}"),
            ex => tcs.TrySetException(ex),
            () => tcs.TrySetResult()
        );

        // TODO: upload will need an actual file
        await manager.Queue(new HttpTransferRequest(
            id,
            uri,
            isUpload,
            $"{FileSystem.AppDataDirectory}/{id}.transfer"
        ));
        await tcs.Task.ConfigureAwait(false);
    }
}


public class TestHttpTransferDelegate : IHttpTransferDelegate
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