using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Net.Http.Infrastructure;
using Shiny.Net.Http.Tests.Fakes;
using Xunit;

namespace Shiny.Net.Http.Tests;


public class DownloadResumeTests
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


    static HttpTransfer NewDownload(string path, DateTimeOffset createdAt, string uri = "https://localhost/file.bin")
    {
        var request = new HttpTransferRequest(
            Guid.NewGuid().ToString(),
            uri,
            TransferType.Download,
            path
        );
        return new HttpTransfer(request, null, 0, HttpTransferState.Pending, createdAt);
    }


    static async Task RunToCompletion(HttpClientHttpTransferProcess process)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Run(() => done.TrySetResult());
        await done.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }


    static string TempPath() => Path.Combine(Path.GetTempPath(), $"shiny-dl-{Guid.NewGuid():N}.bin");


    [Fact(DisplayName = "Download - a file already at the path before queueing is overwritten, not resumed")]
    public async Task PreExistingFile_IsOverwritten()
    {
        var content = Enumerable.Range(0, 500).Select(x => (byte)x).ToArray();
        var path = TempPath();
        try
        {
            await File.WriteAllBytesAsync(path, new byte[300]);
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-5));

            using var handler = new RangeAwareHandler(content);
            var repo = new InMemoryRepository();
            var transfer = NewDownload(path, DateTimeOffset.UtcNow);
            repo.Insert(transfer);

            await RunToCompletion(CreateProcess(repo, handler));

            Assert.Equal(0, handler.RangeRequests);
            Assert.Equal(content, await File.ReadAllBytesAsync(path));
            Assert.Null(repo.Get<HttpTransfer>(transfer.Identifier));
        }
        finally
        {
            TryDelete(path);
        }
    }


    [Fact(DisplayName = "Download - a partial file that is already complete finishes on 416 instead of failing")]
    public async Task CompletePartial_416_Completes()
    {
        var content = Enumerable.Range(0, 400).Select(x => (byte)x).ToArray();
        var path = TempPath();
        try
        {
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await File.WriteAllBytesAsync(path, content); // written by this transfer before the app stopped

            using var handler = new RangeAwareHandler(content);
            var repo = new InMemoryRepository();
            var transfer = NewDownload(path, createdAt);
            repo.Insert(transfer);

            var results = new System.Collections.Concurrent.ConcurrentBag<HttpTransferResult>();
            EventHandler<HttpTransferResult> onProgress = (_, r) => results.Add(r);
            HttpClientHttpTransferProcess.ProgressOccurred += onProgress;
            try
            {
                await RunToCompletion(CreateProcess(repo, handler));
            }
            finally
            {
                HttpClientHttpTransferProcess.ProgressOccurred -= onProgress;
            }

            Assert.Equal(1, handler.RangeRequests);
            Assert.DoesNotContain(results, r => r.Request.Identifier == transfer.Identifier && r.Status == HttpTransferState.Error);
            Assert.Contains(results, r => r.Request.Identifier == transfer.Identifier && r.Status == HttpTransferState.Completed);
            Assert.Equal(content, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            TryDelete(path);
        }
    }


    [Fact(DisplayName = "Loop - a transfer paused while an earlier one runs is not started")]
    public async Task PausedDuringPass_IsNotStarted()
    {
        var content = new byte[100];
        var firstPath = TempPath();
        var secondPath = TempPath();
        try
        {
            using var handler = new RangeAwareHandler(content);
            var repo = new InMemoryRepository();
            var first = NewDownload(firstPath, DateTimeOffset.UtcNow, "https://localhost/first.bin");
            var second = NewDownload(secondPath, DateTimeOffset.UtcNow, "https://localhost/second.bin");
            repo.Insert(first);
            repo.Insert(second);

            // whichever transfer the loop starts first, pause the other one while its request is in flight
            HttpTransfer? other = null;
            handler.OnRequest = uri =>
            {
                if (other != null)
                    return;

                other = uri!.AbsolutePath.Contains("first") ? second : first;
                var current = repo.Get<HttpTransfer>(other.Identifier)!;
                repo.Set(current with { Status = HttpTransferState.Paused });
            };

            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            CreateProcess(repo, handler).Run(() => done.TrySetResult());
            try
            {
                await Task.Delay(400);
                Assert.Equal(1, handler.Requests);
                Assert.Equal(HttpTransferState.Paused, repo.Get<HttpTransfer>(other!.Identifier)!.Status);
                Assert.False(File.Exists(other.Request.LocalFilePath));
            }
            finally
            {
                repo.Clear<HttpTransfer>();
                await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }
        finally
        {
            TryDelete(firstPath);
            TryDelete(secondPath);
        }
    }


    [Theory(DisplayName = "Progress - a zero or unknown (negative) total is not deterministic")]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void ZeroOrNegativeTotal_IsNotDeterministic(long total)
    {
        var progress = new TransferProgress(100, total, 5);
        Assert.False(progress.IsDeterministic);
        Assert.Equal(-1, progress.PercentComplete);
        Assert.Equal(TimeSpan.Zero, progress.EstimatedTimeRemaining);
        Assert.False(TransferProgress.Empty.IsDeterministic);
    }


    static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch { /* best effort */ }
    }


    sealed class RangeAwareHandler(byte[] content) : HttpMessageHandler
    {
        int requests;
        int rangeRequests;
        public int Requests => Volatile.Read(ref this.requests);
        public int RangeRequests => Volatile.Read(ref this.rangeRequests);
        public Action<Uri?>? OnRequest { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref this.requests);
            this.OnRequest?.Invoke(request.RequestUri);

            var from = request.Headers.Range?.Ranges.FirstOrDefault()?.From;
            HttpResponseMessage response;
            if (from == null)
            {
                response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(content) };
            }
            else if (from.Value >= content.Length)
            {
                Interlocked.Increment(ref this.rangeRequests);
                response = new HttpResponseMessage(HttpStatusCode.RequestedRangeNotSatisfiable) { Content = new ByteArrayContent([]) };
                response.Content.Headers.ContentRange = new ContentRangeHeaderValue(content.Length);
            }
            else
            {
                Interlocked.Increment(ref this.rangeRequests);
                var rest = content.Skip((int)from.Value).ToArray();
                response = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new ByteArrayContent(rest) };
                response.Content.Headers.ContentRange = new ContentRangeHeaderValue(from.Value, content.Length - 1, content.Length);
            }
            return Task.FromResult(response);
        }
    }
}
