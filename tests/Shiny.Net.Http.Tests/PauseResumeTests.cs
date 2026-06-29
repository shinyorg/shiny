using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Net.Http.Infrastructure;
using Shiny.Net.Http.Tests.Fakes;
using Xunit;

namespace Shiny.Net.Http.Tests;


public class PauseResumeTests
{
    static HttpClientHttpTransferProcess CreateProcess(InMemoryRepository repo, HttpMessageHandler handler)
        => new(
            NullLogger<HttpClientHttpTransferProcess>.Instance,
            repo,
            new FakeConnectivity(),
            Array.Empty<IHttpTransferDelegate>(),
            new StubHttpClientFactory(handler),
            TimeSpan.FromMilliseconds(50) // short poll so resume is picked up fast
        );


    static HttpTransfer NewDownload(string path, long total)
    {
        var request = new HttpTransferRequest(
            Guid.NewGuid().ToString(),
            "https://localhost/file.bin",
            TransferType.Download,
            path
        );
        return new HttpTransfer(request, total, 0, HttpTransferState.Pending, DateTimeOffset.UtcNow);
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


    [Fact(DisplayName = "Pause/Resume - download pauses without cancelling, keeps partial file, resumes via Range and completes")]
    public async Task Download_Pause_Preserves_Partial_Then_Resume_Completes()
    {
        var full = new byte[2000];
        new Random(7).NextBytes(full);
        const int pauseAt = 600;

        using var handler = new ControllableDownloadHandler(full, pauseAt);
        var repo = new InMemoryRepository();
        var path = Path.Combine(Path.GetTempPath(), $"shiny-dl-{Guid.NewGuid():N}.bin");

        var process = CreateProcess(repo, handler);
        var transfer = NewDownload(path, full.Length);
        repo.Insert(transfer);

        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Run(() => done.TrySetResult());

        try
        {
            // download is mid-stream and blocked after the first chunk
            await handler.FirstChunkServed.WaitAsync(TimeSpan.FromSeconds(5));

            // PAUSE: flip status (exactly what IHttpTransferManager.Pause does). The process
            // interrupts the in-flight read without removing the transfer.
            repo.Set(repo.Get<HttpTransfer>(transfer.Identifier)! with { Status = HttpTransferState.Paused });

            await WaitFor(
                () => repo.Get<HttpTransfer>(transfer.Identifier)?.Status == HttpTransferState.Paused,
                TimeSpan.FromSeconds(5)
            );
            // partial bytes flushed on unwind, but the download is not complete and was not removed
            await WaitFor(() => File.Exists(path) && new FileInfo(path).Length >= pauseAt, TimeSpan.FromSeconds(5));

            Assert.NotNull(repo.Get<HttpTransfer>(transfer.Identifier));
            var partialLen = new FileInfo(path).Length;
            Assert.InRange(partialLen, pauseAt, full.Length - 1);
            Assert.Equal(0, handler.RangeRequestCount); // no resume attempted yet

            // RESUME: flip back to Pending; the loop continues from the partial file via a Range request
            repo.Set(repo.Get<HttpTransfer>(transfer.Identifier)! with { Status = HttpTransferState.Pending });

            // completion removes the transfer from the repository
            await WaitFor(() => repo.Get<HttpTransfer>(transfer.Identifier) == null, TimeSpan.FromSeconds(10));
            await done.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(1, handler.RangeRequestCount);          // resumed via exactly one Range request
            Assert.Equal(full, await File.ReadAllBytesAsync(path)); // reassembled file is byte-identical
        }
        finally
        {
            TryDelete(path);
        }
    }


    [Fact(DisplayName = "Pause/Resume - a paused transfer is skipped by the loop (no HTTP request issued)")]
    public async Task Paused_Transfer_Is_Skipped()
    {
        var full = new byte[100];
        using var handler = new ControllableDownloadHandler(full, 10);
        var repo = new InMemoryRepository();
        var path = Path.Combine(Path.GetTempPath(), $"shiny-dl-{Guid.NewGuid():N}.bin");

        var process = CreateProcess(repo, handler);

        // seed an already-paused transfer
        var transfer = NewDownload(path, full.Length) with { Status = HttpTransferState.Paused };
        repo.Insert(transfer);

        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Run(() => done.TrySetResult());
        try
        {
            // let the loop spin several times
            await Task.Delay(300);

            Assert.Equal(0, handler.TotalRequestCount); // never started
            Assert.Equal(HttpTransferState.Paused, repo.Get<HttpTransfer>(transfer.Identifier)!.Status);
            Assert.False(File.Exists(path));
        }
        finally
        {
            repo.Clear<HttpTransfer>(); // unblocks the loop -> onComplete
            await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
            TryDelete(path);
        }
    }


    static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch { /* best effort */ }
    }
}
