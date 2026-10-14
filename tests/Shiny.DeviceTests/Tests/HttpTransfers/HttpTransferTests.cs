using System.Net.Http.Json;
using Shiny.Net.Http;

namespace Shiny.DeviceTests.Tests;


/// <summary>
/// Background uploads/downloads against the runner's /files fixture (HttpTransferFixture). iOS and Mac Catalyst
/// run through the background NSURLSession; Android through the dataSync foreground service + HttpClient loop.
/// </summary>
public class HttpTransferTests(
    IHttpTransferManager manager,
    IPlatform platform
) : DeviceTestSuite
{
    public override string Name => "HttpTransfers";

    string directory = null!;


    public override async Task SetupAsync(TestContext context)
    {
        context.RequireCapability(HostCapability.Server);

        await manager.CancelAll();
        await context.Host.DeleteAsync("files");

        this.directory = Path.Combine(platform.Cache.FullName, "devicetests-http");
        if (Directory.Exists(this.directory))
            Directory.Delete(this.directory, true);
        Directory.CreateDirectory(this.directory);
    }


    public override async Task TeardownAsync(TestContext context)
    {
        await manager.CancelAll();
        if (Directory.Exists(this.directory))
            Directory.Delete(this.directory, true);
    }


    protected override void Define()
    {
        this.Test("Download_ContentMatches", async ctx =>
        {
            const int size = 256 * 1024;
            var request = this.Download(ctx, $"files/download/{size}");

            using var watcher = new TransferWatcher(manager, request.Identifier);
            await manager.Queue(request);

            var result = await watcher.WaitForTerminalAsync(ctx);
            Assert.Equal(HttpTransferState.Completed, result.Status);
            await ctx.Events.WaitAsync<HttpTransferRequest>(TestHttpTransferDelegate.CompletedTopic, TimeSpan.FromSeconds(10), ctx.CancellationToken, x => x.Identifier == request.Identifier);

            await AssertDownloadedPattern(request.LocalFilePath, size);
            Assert.DoesNotContain(await manager.GetTransfers(), x => x.Identifier == request.Identifier);
        });


        this.Test("Download_ReportsProgress", async ctx =>
        {
            const int size = 768 * 1024;
            // ~3.6s of trickled body so both the native and the managed (2s window) paths report mid-flight
            var request = this.Download(ctx, $"files/download/{size}?chunk=32768&delayMs=150");

            using var watcher = new TransferWatcher(manager, request.Identifier);
            await manager.Queue(request);
            var result = await watcher.WaitForTerminalAsync(ctx);
            Assert.Equal(HttpTransferState.Completed, result.Status);

            var progress = watcher.Updates.Where(x => x.Status == HttpTransferState.InProgress).Select(x => x.Progress).ToList();
            ctx.Log($"{progress.Count} progress updates: {String.Join(", ", progress.Select(x => x.BytesTransferred))}");

            Assert.Contains(progress, x => x.BytesTransferred > 0 && x.BytesTransferred < size);
            Assert.Contains(progress, x => x.BytesToTransfer == size);
            for (var i = 1; i < progress.Count; i++)
                Assert.True(progress[i].BytesTransferred >= progress[i - 1].BytesTransferred, "Progress went backwards");
        });


        this.Test("Download_HonoursHeadersAndMethod", async ctx =>
        {
            var request = this.Download(ctx, "files/download/4096", headers: new Dictionary<string, string> { ["X-Shiny-Test"] = "headers-123" });
            request.HttpMethod = "POST";

            using var watcher = new TransferWatcher(manager, request.Identifier);
            await manager.Queue(request);
            Assert.Equal(HttpTransferState.Completed, (await watcher.WaitForTerminalAsync(ctx)).Status);

            var seen = await GetRequests(ctx, request.Identifier);
            var first = Assert.Single(seen);
            Assert.Equal("POST", first.Method);
            Assert.True(first.Headers.TryGetValue("X-Shiny-Test", out var value), "Custom header did not reach the server");
            Assert.Equal("headers-123", value);
        });


        this.Test("Download_ServerError_ReportsError", async ctx =>
        {
            var request = this.Download(ctx, "files/status/500");
            using var watcher = new TransferWatcher(manager, request.Identifier);
            await manager.Queue(request);

            var result = await watcher.WaitForTerminalAsync(ctx);
            Assert.Equal(HttpTransferState.Error, result.Status);

            var failure = await ctx.Events.WaitAsync<HttpTransferFailure>(TestHttpTransferDelegate.ErrorTopic, TimeSpan.FromSeconds(10), ctx.CancellationToken, x => x.Request.Identifier == request.Identifier);
            Assert.Equal(500, failure.StatusCode);
            Assert.Empty(ctx.Events.Get<HttpTransferRequest>(TestHttpTransferDelegate.CompletedTopic));
        });


        this.Test("Download_NotFound_ReportsError", async ctx =>
        {
            var request = this.Download(ctx, "files/status/404");
            using var watcher = new TransferWatcher(manager, request.Identifier);
            await manager.Queue(request);

            Assert.Equal(HttpTransferState.Error, (await watcher.WaitForTerminalAsync(ctx)).Status);
            var failure = await ctx.Events.WaitAsync<HttpTransferFailure>(TestHttpTransferDelegate.ErrorTopic, TimeSpan.FromSeconds(10), ctx.CancellationToken, x => x.Request.Identifier == request.Identifier);
            Assert.Equal(404, failure.StatusCode);
        });


        this.Test("Download_Cancel_StopsTransfer", async ctx =>
        {
            var request = this.SlowDownload(ctx);
            using var watcher = new TransferWatcher(manager, request.Identifier);
            await manager.Queue(request);

            await watcher.WaitForAsync(x => x.Status == HttpTransferState.InProgress && x.Progress.BytesTransferred > 0, TimeSpan.FromSeconds(30), ctx);
            await manager.Cancel(request.Identifier);

            var result = await watcher.WaitForTerminalAsync(ctx, TimeSpan.FromSeconds(20));
            Assert.Equal(HttpTransferState.Canceled, result.Status);
            Assert.DoesNotContain(await manager.GetTransfers(), x => x.Identifier == request.Identifier);

            // nothing completes behind the cancel
            await Task.Delay(2000, ctx.CancellationToken);
            Assert.Empty(ctx.Events.Get<HttpTransferRequest>(TestHttpTransferDelegate.CompletedTopic));
        });


        // the skill documents that a cancelled download deletes its partial file so a re-queue starts fresh
        this.Test("Download_Cancel_RemovesPartialFile", async ctx =>
        {
            var request = this.SlowDownload(ctx);
            using var watcher = new TransferWatcher(manager, request.Identifier);
            await manager.Queue(request);

            await watcher.WaitForAsync(x => x.Status == HttpTransferState.InProgress && x.Progress.BytesTransferred > 0, TimeSpan.FromSeconds(30), ctx);
            await manager.Cancel(request.Identifier);
            await watcher.WaitForTerminalAsync(ctx, TimeSpan.FromSeconds(20));

            await Task.Delay(1500, ctx.CancellationToken);
            // the message is built eagerly, so it must not touch a file that (correctly) isn't there
            var exists = File.Exists(request.LocalFilePath);
            Assert.False(exists, exists ? $"Partial download left behind ({new FileInfo(request.LocalFilePath).Length} bytes)" : null);
        });


        // a stale file at the destination must be replaced, not treated as a partial download to resume
        this.Test("Download_OverExistingFile_ReplacesContent", async ctx =>
        {
            const int size = 128 * 1024;
            var request = this.Download(ctx, $"files/download/{size}");
            await File.WriteAllBytesAsync(request.LocalFilePath, Enumerable.Repeat((byte)0xEE, 1000).ToArray(), ctx.CancellationToken);

            using var watcher = new TransferWatcher(manager, request.Identifier);
            await manager.Queue(request);
            Assert.Equal(HttpTransferState.Completed, (await watcher.WaitForTerminalAsync(ctx)).Status);

            foreach (var r in await GetRequests(ctx, request.Identifier))
                ctx.Log($"{r.Method} Range='{r.Range}'");

            await AssertDownloadedPattern(request.LocalFilePath, size);
        });


        // the runner drops the first connection half way: the managed loop must resume with a Range request
        // on its next pass (~10s); NSURLSession may instead report the failure
        this.Test("Download_ConnectionDrop_ResumesOrReportsError", async ctx =>
        {
            const int size = 512 * 1024;
            var id = NewId();
            var request = this.Download(ctx, $"files/flaky/{size}?dropAfter={128 * 1024}", id);

            using var watcher = new TransferWatcher(manager, request.Identifier);
            await manager.Queue(request);
            var result = await watcher.WaitForTerminalAsync(ctx, TimeSpan.FromSeconds(100));

            var seen = await GetRequests(ctx, request.Identifier);
            ctx.Log($"terminal={result.Status} requests={String.Join(" | ", seen.Select(x => $"{x.Method} Range='{x.Range}'"))}");

            if (result.Status == HttpTransferState.Error)
            {
                Assert.True(ctx.IsApple, "Only NSURLSession may surface a dropped connection as an error - the managed loop retries");
                await ctx.Events.WaitAsync<HttpTransferFailure>(TestHttpTransferDelegate.ErrorTopic, TimeSpan.FromSeconds(10), ctx.CancellationToken, x => x.Request.Identifier == request.Identifier);
            }
            else
            {
                Assert.Equal(HttpTransferState.Completed, result.Status);
                await AssertDownloadedPattern(request.LocalFilePath, size);
                if (ctx.IsAndroid)
                    Assert.Contains(seen.Skip(1), x => x.Range?.StartsWith("bytes=", StringComparison.Ordinal) == true);
            }
        }, TimeSpan.FromSeconds(120));


        this.Test("Download_Concurrent_AllComplete", async ctx =>
        {
            int[] sizes = [64 * 1024, 192 * 1024, 320 * 1024];
            var requests = sizes.Select(x => this.Download(ctx, $"files/download/{x}")).ToList();
            var watchers = requests.Select(x => new TransferWatcher(manager, x.Identifier)).ToList();
            try
            {
                foreach (var request in requests)
                    await manager.Queue(request);

                var results = await Task.WhenAll(watchers.Select(x => x.WaitForTerminalAsync(ctx, TimeSpan.FromSeconds(90))));
                Assert.All(results, x => Assert.Equal(HttpTransferState.Completed, x.Status));

                for (var i = 0; i < sizes.Length; i++)
                    await AssertDownloadedPattern(requests[i].LocalFilePath, sizes[i]);
            }
            finally
            {
                watchers.ForEach(x => x.Dispose());
            }
        }, TimeSpan.FromSeconds(100));


        this.Test("Upload_Raw_ServerReceivesExactBytes", async ctx =>
        {
            var (path, sha, size) = await this.CreateUploadFile(300 * 1024);
            var id = NewId();
            var request = new HttpTransferRequest(id, ctx.Host.Url($"files/upload/{id}"), TransferType.UploadRaw, path);

            using var watcher = new TransferWatcher(manager, id);
            await manager.Queue(request);
            Assert.Equal(HttpTransferState.Completed, (await watcher.WaitForTerminalAsync(ctx)).Status);
            await ctx.Events.WaitAsync<HttpTransferRequest>(TestHttpTransferDelegate.CompletedTopic, TimeSpan.FromSeconds(10), ctx.CancellationToken, x => x.Identifier == id);

            var record = await GetUpload(ctx, id);
            Assert.Equal("POST", record.Method);
            Assert.Equal(size, record.Length);
            Assert.Equal(sha, record.Sha256);
            Assert.Equal(1, record.Count);
        });


        this.Test("Upload_Multipart_ServerReceivesFileAndFields", async ctx =>
        {
            var (path, sha, size) = await this.CreateUploadFile(200 * 1024);
            var id = NewId();
            var request = new HttpTransferRequest(
                id,
                ctx.Host.Url($"files/upload/{id}"),
                TransferType.UploadMultipart,
                path,
                HttpContent: new TransferHttpContent("hello from shiny") { ContentFormDataName = "note" }
            )
            {
                FileFormDataName = "upload"
            };

            using var watcher = new TransferWatcher(manager, id);
            await manager.Queue(request);
            Assert.Equal(HttpTransferState.Completed, (await watcher.WaitForTerminalAsync(ctx)).Status);

            var record = await GetUpload(ctx, id);
            ctx.Log($"content-type={record.ContentType} field={record.FileFieldName} file={record.FileName} form=[{String.Join(", ", record.Form.Select(x => $"{x.Key}={x.Value}"))}]");
            Assert.StartsWith("multipart/form-data", record.ContentType, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("upload", record.FileFieldName);
            Assert.Equal(Path.GetFileName(path), record.FileName);
            Assert.Equal(size, record.Length);
            Assert.Equal(sha, record.Sha256);
            Assert.True(record.Form.TryGetValue("note", out var note), "The HttpContent form part did not arrive");
            Assert.Equal("hello from shiny", note);
        });


        this.Test("Upload_PutWithHeaders", async ctx =>
        {
            var (path, sha, _) = await this.CreateUploadFile(32 * 1024);
            var id = NewId();
            var request = new HttpTransferRequest(
                id,
                ctx.Host.Url($"files/upload/{id}"),
                TransferType.UploadRaw,
                path,
                Headers: new Dictionary<string, string> { ["X-Shiny-Test"] = "upload-456" }
            )
            {
                HttpMethod = "PUT"
            };

            using var watcher = new TransferWatcher(manager, id);
            await manager.Queue(request);
            Assert.Equal(HttpTransferState.Completed, (await watcher.WaitForTerminalAsync(ctx)).Status);

            var record = await GetUpload(ctx, id);
            Assert.Equal("PUT", record.Method);
            Assert.Equal(sha, record.Sha256);
            Assert.True(record.Headers.TryGetValue("X-Shiny-Test", out var value), "Custom header did not reach the server");
            Assert.Equal("upload-456", value);
        });


        this.Test("Upload_ReportsProgress", async ctx =>
        {
            var (path, _, size) = await this.CreateUploadFile(2 * 1024 * 1024);
            var id = NewId();
            var request = new HttpTransferRequest(id, ctx.Host.Url($"files/upload/{id}"), TransferType.UploadRaw, path);

            using var watcher = new TransferWatcher(manager, id);
            await manager.Queue(request);
            Assert.Equal(HttpTransferState.Completed, (await watcher.WaitForTerminalAsync(ctx)).Status);

            var progress = watcher.Updates.Where(x => x.Status == HttpTransferState.InProgress).Select(x => x.Progress).ToList();
            ctx.Log($"{progress.Count} progress updates, max {progress.Select(x => x.BytesTransferred).DefaultIfEmpty().Max()} of {size}");
            Assert.Contains(progress, x => x.BytesTransferred > 0);
            Assert.All(progress, x => Assert.True(x.BytesTransferred <= size));
        });


        this.Test("Upload_ServerError_ReportsError", async ctx =>
        {
            var (path, _, _) = await this.CreateUploadFile(16 * 1024);
            var id = NewId();
            var request = new HttpTransferRequest(id, ctx.Host.Url($"files/status/500?id={id}"), TransferType.UploadRaw, path);

            using var watcher = new TransferWatcher(manager, id);
            await manager.Queue(request);
            Assert.Equal(HttpTransferState.Error, (await watcher.WaitForTerminalAsync(ctx)).Status);

            var failure = await ctx.Events.WaitAsync<HttpTransferFailure>(TestHttpTransferDelegate.ErrorTopic, TimeSpan.FromSeconds(10), ctx.CancellationToken, x => x.Request.Identifier == id);
            Assert.Equal(500, failure.StatusCode);
        });


        this.Test("Queue_MissingUploadFile_Throws", async ctx =>
        {
            var id = NewId();
            var request = new HttpTransferRequest(id, ctx.Host.Url($"files/upload/{id}"), TransferType.UploadRaw, Path.Combine(this.directory, "does-not-exist.bin"));
            await Assert.ThrowsAnyAsync<Exception>(() => manager.Queue(request));
            Assert.DoesNotContain(await manager.GetTransfers(), x => x.Identifier == id);
        });
    }


    HttpTransferRequest Download(TestContext ctx, string relative, string? id = null, Dictionary<string, string>? headers = null)
    {
        id ??= NewId();
        var url = ctx.Host.Url(relative);
        url += (url.Contains('?') ? "&" : "?") + "id=" + id;
        return new HttpTransferRequest(id, url, TransferType.Download, Path.Combine(this.directory, id + ".bin"), Headers: headers);
    }


    // 2MB trickled at 16KB / 200ms (~25s) - long enough to cancel mid-flight
    HttpTransferRequest SlowDownload(TestContext ctx) => this.Download(ctx, $"files/download/{2 * 1024 * 1024}?chunk=16384&delayMs=200");


    async Task<(string Path, string Sha256, long Size)> CreateUploadFile(int size)
    {
        var bytes = FilePattern.Create(size);
        var path = Path.Combine(this.directory, $"upload-{NewId()}.bin");
        await File.WriteAllBytesAsync(path, bytes);
        return (path, FilePattern.Sha256(bytes), size);
    }


    static async Task AssertDownloadedPattern(string path, long size)
    {
        Assert.True(File.Exists(path), $"Download not found at {path}");
        Assert.Equal(size, new FileInfo(path).Length);
        Assert.Equal(FilePattern.Sha256(FilePattern.Create(size)), await FilePattern.Sha256Async(path));
    }


    static async Task<List<RecordedRequest>> GetRequests(TestContext ctx, string id)
    {
        var response = await ctx.Host.GetAsync($"files/requests/{id}", ctx.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync(HttpFixtureJsonContext.Default.ListRecordedRequest, ctx.CancellationToken) ?? [];
    }


    static async Task<UploadRecord> GetUpload(TestContext ctx, string id)
    {
        var response = await ctx.Host.GetAsync($"files/upload/{id}", ctx.CancellationToken);
        Assert.True(response.IsSuccessStatusCode, $"The runner never received upload {id} ({(int)response.StatusCode})");
        return (await response.Content.ReadFromJsonAsync(HttpFixtureJsonContext.Default.UploadRecord, ctx.CancellationToken))!;
    }


    static string NewId() => "dt-" + Guid.NewGuid().ToString("N")[..12];
}


/// <summary>
/// Collects every UpdateReceived for one transfer from before it is queued, so nothing is missed.
/// </summary>
sealed class TransferWatcher : IDisposable
{
    readonly IHttpTransferManager manager;
    readonly string identifier;
    readonly List<HttpTransferResult> updates = [];
    readonly TaskCompletionSource<HttpTransferResult> terminal = new(TaskCreationOptions.RunContinuationsAsynchronously);


    public TransferWatcher(IHttpTransferManager manager, string identifier)
    {
        this.manager = manager;
        this.identifier = identifier;
        manager.UpdateReceived += this.OnUpdate;
    }


    public IReadOnlyList<HttpTransferResult> Updates
    {
        get { lock (this.updates) return this.updates.ToList(); }
    }


    public async Task<HttpTransferResult> WaitForTerminalAsync(TestContext ctx, TimeSpan? timeout = null)
    {
        var span = timeout ?? TimeSpan.FromSeconds(45);
        try
        {
            return await this.terminal.Task.WaitAsync(span, ctx.CancellationToken);
        }
        catch (TimeoutException)
        {
            var last = this.Updates.LastOrDefault();
            throw new TimeoutException($"Transfer {this.identifier} did not finish within {span.TotalSeconds:0}s - last update: {last?.Status} {last?.Progress.BytesTransferred}/{last?.Progress.BytesToTransfer}");
        }
    }


    public async Task WaitForAsync(Func<HttpTransferResult, bool> predicate, TimeSpan timeout, TestContext ctx)
        => await ctx.WaitUntilAsync(() => this.Updates.Any(predicate), timeout, $"a matching update for transfer {this.identifier}");


    void OnUpdate(object? sender, HttpTransferResult result)
    {
        if (!result.Request.Identifier.Equals(this.identifier, StringComparison.OrdinalIgnoreCase))
            return;

        lock (this.updates)
            this.updates.Add(result);

        if (result.Status is HttpTransferState.Completed or HttpTransferState.Canceled or HttpTransferState.Error)
            this.terminal.TrySetResult(result);
    }


    public void Dispose() => this.manager.UpdateReceived -= this.OnUpdate;
}
