using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Net.Http.Infrastructure;
using Shiny.Net.Http.Tests.Fakes;
using Xunit;

namespace Shiny.Net.Http.Tests;


public class TusUploadTests
{
    static HttpClientHttpTransferProcess CreateProcess(InMemoryRepository repo, HttpMessageHandler handler)
        => new(
            NullLogger<HttpClientHttpTransferProcess>.Instance,
            repo,
            new FakeConnectivity(),
            new ServiceCollection().BuildServiceProvider(),
            new StubHttpClientFactory(handler),
            TimeSpan.FromMilliseconds(50)
        );


    static HttpTransfer NewTus(TempFile file, long? chunkSize = null)
    {
        var builder = new TusUploadRequest(file.Path).WithEndpoint(FakeTusServer.Endpoint);
        if (chunkSize != null)
            builder.WithChunkSize(chunkSize.Value);

        return new HttpTransfer(builder.Build(), file.Length, 0, HttpTransferState.Pending, DateTimeOffset.UtcNow);
    }


    static async Task RunToCompletion(HttpClientHttpTransferProcess process, InMemoryRepository repo, HttpTransfer transfer, Func<Task>? whileRunning = null)
    {
        repo.Insert(transfer);
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Run(() => done.TrySetResult());

        if (whileRunning != null)
            await whileRunning();

        await WaitFor(() => repo.Get<HttpTransfer>(transfer.Identifier) == null, TimeSpan.FromSeconds(10));
        await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }


    static async Task WaitFor(Func<bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (predicate())
                return;
            await Task.Delay(20);
        }
        throw new TimeoutException("Condition was not met within " + timeout);
    }


    static FakeTusServer.Upload SingleUpload(FakeTusServer server)
        => Assert.Single(server.Uploads).Value;


    [Fact(DisplayName = "tus - creates the upload, sends every chunk, and the server holds the exact file")]
    public async Task Uploads_In_Chunks()
    {
        using var file = new TempFile(250_000);
        using var server = new FakeTusServer();
        var repo = new InMemoryRepository();

        await RunToCompletion(CreateProcess(repo, server), repo, NewTus(file, chunkSize: 100_000));

        var upload = SingleUpload(server);
        Assert.Equal(file.Length, upload.Length);
        Assert.Equal(await File.ReadAllBytesAsync(file.Path), upload.Data.ToArray());
        Assert.Equal(1, server.Count("POST"));
        Assert.Equal(3, server.Count("PATCH"));
        Assert.Equal(Path.GetFileName(file.Path), FakeTusServer.DecodeMetadata(upload.Metadata!, "filename"));
    }


    [Fact(DisplayName = "tus - without a chunk size the file goes in one PATCH")]
    public async Task Uploads_In_One_Patch()
    {
        using var file = new TempFile(50_000);
        using var server = new FakeTusServer();
        var repo = new InMemoryRepository();

        await RunToCompletion(CreateProcess(repo, server), repo, NewTus(file));

        Assert.Equal(1, server.Count("PATCH"));
        Assert.Equal(await File.ReadAllBytesAsync(file.Path), SingleUpload(server).Data.ToArray());
    }


    [Fact(DisplayName = "tus - a dropped connection keeps the transfer and resumes from the server's offset")]
    public async Task Network_Drop_Resumes_From_Server_Offset()
    {
        using var file = new TempFile(200_000);
        using var server = new FakeTusServer { DropNextPatchAfter = 70_000 };
        var repo = new InMemoryRepository();

        await RunToCompletion(CreateProcess(repo, server), repo, NewTus(file));

        Assert.Equal(1, server.Count("POST"));   // the upload was not created twice
        Assert.Equal(1, server.Count("HEAD"));   // the retry asked where to continue
        Assert.Equal(2, server.Count("PATCH"));
        Assert.Equal(await File.ReadAllBytesAsync(file.Path), SingleUpload(server).Data.ToArray());
    }


    [Fact(DisplayName = "tus - pause interrupts the PATCH; resume continues from the server's offset")]
    public async Task Pause_Then_Resume()
    {
        using var file = new TempFile(200_000);
        using var server = new FakeTusServer { StallNextPatchAfter = 60_000 };
        var repo = new InMemoryRepository();
        var transfer = NewTus(file);

        await RunToCompletion(CreateProcess(repo, server), repo, transfer, async () =>
        {
            await server.PatchStalled.WaitAsync(TimeSpan.FromSeconds(5));

            repo.Set(repo.Get<HttpTransfer>(transfer.Identifier)! with { Status = HttpTransferState.Paused });
            await Task.Delay(200);

            var paused = repo.Get<HttpTransfer>(transfer.Identifier)!;
            Assert.Equal(HttpTransferState.Paused, paused.Status);
            Assert.NotNull(paused.TusUploadUri);                   // survives the pause
            Assert.StartsWith("https://tus.local/files/", paused.TusUploadUri); // relative Location resolved
            Assert.Equal(60_000, SingleUpload(server).Data.Length);

            repo.Set(paused with { Status = HttpTransferState.Pending });
        });

        Assert.Equal(1, server.Count("POST"));
        Assert.Equal(1, server.Count("HEAD"));
        Assert.Equal(await File.ReadAllBytesAsync(file.Path), SingleUpload(server).Data.ToArray());
    }


    [Fact(DisplayName = "tus - an upload the server no longer knows is created again")]
    public async Task Expired_Upload_Is_Recreated()
    {
        using var file = new TempFile(10_000);
        using var server = new FakeTusServer();
        var repo = new InMemoryRepository();
        var transfer = NewTus(file) with { TusUploadUri = FakeTusServer.Endpoint + "gone" };

        await RunToCompletion(CreateProcess(repo, server), repo, transfer);

        Assert.Equal(1, server.Count("HEAD"));
        Assert.Equal(1, server.Count("POST"));
        Assert.Equal(await File.ReadAllBytesAsync(file.Path), SingleUpload(server).Data.ToArray());
    }


    [Fact(DisplayName = "tus - an HTTP error from the server fails the transfer")]
    public async Task Server_Error_Fails_Transfer()
    {
        using var file = new TempFile(1_000);
        var repo = new InMemoryRepository();
        var transfer = NewTus(file);

        HttpTransferResult? error = null;
        EventHandler<HttpTransferResult> handler = (_, r) =>
        {
            if (r.Request.Identifier == transfer.Identifier && r.Status == HttpTransferState.Error)
                error = r;
        };
        HttpClientHttpTransferProcess.ProgressOccurred += handler;
        try
        {
            await RunToCompletion(CreateProcess(repo, new ForbiddenHandler()), repo, transfer);
            Assert.NotNull(error);
        }
        finally
        {
            HttpClientHttpTransferProcess.ProgressOccurred -= handler;
        }
    }


    [Fact(DisplayName = "tus - metadata is encoded as key base64(value) pairs")]
    public void Metadata_Encoding()
    {
        var header = TusProtocol.EncodeMetadata(new System.Collections.Generic.Dictionary<string, string>
        {
            ["filename"] = "photo.jpg",
            ["is_confidential"] = ""
        });
        Assert.Equal("filename cGhvdG8uanBn,is_confidential", header);
        Assert.Throws<ArgumentException>(() => TusProtocol.EncodeMetadata(new System.Collections.Generic.Dictionary<string, string> { ["bad key"] = "x" }));
    }


    [Fact(DisplayName = "tus - builder produces an UploadTus request with metadata, headers and chunk size")]
    public void Builder()
    {
        using var file = new TempFile();
        var request = new TusUploadRequest(file.Path)
            .WithEndpoint(FakeTusServer.Endpoint)
            .WithMetadata("filetype", "application/octet-stream")
            .WithBearerToken("abc")
            .WithChunkSize(5_000_000)
            .Build();

        Assert.Equal(TransferType.UploadTus, request.Type);
        Assert.True(request.Type.IsUpload());
        Assert.Equal(5_000_000, request.TusChunkSize);
        Assert.Equal("Bearer abc", request.Headers!["Authorization"]);
        Assert.Equal(Path.GetFileName(file.Path), request.TusMetadata!["filename"]);
        Assert.Equal("application/octet-stream", request.TusMetadata["filetype"]);

        Assert.Throws<InvalidOperationException>(() => new TusUploadRequest(file.Path).Build());
        Assert.False(new TusUploadRequest(file.Path).WithEndpoint(FakeTusServer.Endpoint).WithoutFileName().Build().TusMetadata!.ContainsKey("filename"));
    }


    sealed class ForbiddenHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden));
    }
}
