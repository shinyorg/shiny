using System;
using System.Collections.Concurrent;
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


/// <summary>
/// Download resume validates the server file with If-Range: the first response's strong ETag (or Last-Modified)
/// is stored beside the transfer, a resume sends it, and a 200 back means the file changed and restarts the download.
/// Each test drops the first response's connection part way through, so the loop pauses the transfer and resumes it
/// on its next pass, exactly as a network drop would.
/// </summary>
public class DownloadValidatorTests
{
    const int DropAfter = 400;
    static readonly DateTimeOffset V1Modified = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);


    static HttpClientHttpTransferProcess CreateProcess(InMemoryRepository repo, HttpMessageHandler handler)
        => new(
            NullLogger<HttpClientHttpTransferProcess>.Instance,
            repo,
            new FakeConnectivity(),
            new ServiceCollection().BuildServiceProvider(),
            new StubHttpClientFactory(handler),
            TimeSpan.FromMilliseconds(50)
        );


    static HttpTransfer NewDownload(string path)
    {
        var request = new HttpTransferRequest(
            Guid.NewGuid().ToString(),
            "https://localhost/file.bin",
            TransferType.Download,
            path
        );
        // created a little before the partial file is written, so the resume check accepts it
        return new HttpTransfer(request, null, 0, HttpTransferState.Pending, DateTimeOffset.UtcNow.AddSeconds(-2));
    }


    static async Task RunToCompletion(HttpClientHttpTransferProcess process)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Run(() => done.TrySetResult());
        await done.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }


    static byte[] Bytes(int length, int seed) => Enumerable.Range(0, length).Select(x => (byte)(x * seed)).ToArray();
    static string TempPath() => Path.Combine(Path.GetTempPath(), $"shiny-dlv-{Guid.NewGuid():N}.bin");


    [Fact(DisplayName = "Validator - a file that changed between attempts restarts and ends with the new file")]
    public async Task FileChanged_Restarts()
    {
        var path = TempPath();
        try
        {
            var v2 = Bytes(1200, 7);
            using var server = new ValidatingServer(Bytes(1000, 3), "\"v1\"", null);
            var repo = new InMemoryRepository();
            var transfer = NewDownload(path);
            repo.Insert(transfer);

            TransferValidator? storedBeforeResume = null;
            server.OnRequest = n =>
            {
                if (n != 2)
                    return;

                storedBeforeResume = repo.Get<TransferValidator>(transfer.Identifier);
                server.Content = v2;
                server.ETag = "\"v2\"";
            };

            await RunToCompletion(CreateProcess(repo, server));

            Assert.Equal("\"v1\"", storedBeforeResume?.ETag);
            var resume = server.Requests.ElementAt(1);
            Assert.Equal(DropAfter, resume.RangeFrom);
            Assert.Equal("\"v1\"", resume.IfRange);
            Assert.Equal(HttpStatusCode.OK, resume.Status);
            Assert.Equal(v2, await File.ReadAllBytesAsync(path));
            Assert.Null(repo.Get<HttpTransfer>(transfer.Identifier));
            Assert.Null(repo.Get<TransferValidator>(transfer.Identifier));
        }
        finally
        {
            TryDelete(path);
        }
    }


    [Fact(DisplayName = "Validator - an unchanged file continues with a 206 and the resume carries If-Range")]
    public async Task Unchanged_Continues()
    {
        var path = TempPath();
        try
        {
            var content = Bytes(1000, 3);
            using var server = new ValidatingServer(content, "\"v1\"", V1Modified);
            var repo = new InMemoryRepository();
            var transfer = NewDownload(path);
            repo.Insert(transfer);

            await RunToCompletion(CreateProcess(repo, server));

            Assert.Equal(2, server.Requests.Count);
            var resume = server.Requests.ElementAt(1);
            Assert.Equal(DropAfter, resume.RangeFrom);
            Assert.Equal("\"v1\"", resume.IfRange);
            Assert.Equal(HttpStatusCode.PartialContent, resume.Status);
            Assert.Equal(content, await File.ReadAllBytesAsync(path));
            Assert.Null(repo.Get<TransferValidator>(transfer.Identifier));
        }
        finally
        {
            TryDelete(path);
        }
    }


    [Fact(DisplayName = "Validator - a weak ETag is not used; the resume falls back to Last-Modified")]
    public async Task WeakETag_FallsBackToLastModified()
    {
        var path = TempPath();
        try
        {
            var content = Bytes(1000, 3);
            using var server = new ValidatingServer(content, "W/\"v1\"", V1Modified);
            var repo = new InMemoryRepository();
            var transfer = NewDownload(path);
            repo.Insert(transfer);

            TransferValidator? storedBeforeResume = null;
            server.OnRequest = n =>
            {
                if (n == 2)
                    storedBeforeResume = repo.Get<TransferValidator>(transfer.Identifier);
            };

            await RunToCompletion(CreateProcess(repo, server));

            Assert.NotNull(storedBeforeResume);
            Assert.Null(storedBeforeResume!.ETag);
            Assert.Equal(V1Modified, storedBeforeResume.LastModified);

            var resume = server.Requests.ElementAt(1);
            Assert.Equal(V1Modified, resume.IfRangeDate);
            Assert.Equal(HttpStatusCode.PartialContent, resume.Status);
            Assert.Equal(content, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            TryDelete(path);
        }
    }


    [Fact(DisplayName = "Validator - a Last-Modified that changed restarts the download")]
    public async Task LastModifiedChanged_Restarts()
    {
        var path = TempPath();
        try
        {
            var v2 = Bytes(900, 11);
            using var server = new ValidatingServer(Bytes(1000, 3), null, V1Modified);
            var repo = new InMemoryRepository();
            var transfer = NewDownload(path);
            repo.Insert(transfer);

            server.OnRequest = n =>
            {
                if (n != 2)
                    return;

                server.Content = v2;
                server.LastModified = V1Modified.AddHours(1);
            };

            await RunToCompletion(CreateProcess(repo, server));

            var resume = server.Requests.ElementAt(1);
            Assert.Equal(V1Modified, resume.IfRangeDate);
            Assert.Equal(HttpStatusCode.OK, resume.Status);
            Assert.Equal(v2, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            TryDelete(path);
        }
    }


    [Fact(DisplayName = "Validator - with no validator the resume sends a plain Range, as before")]
    public async Task NoValidator_PlainRange()
    {
        var path = TempPath();
        try
        {
            var content = Bytes(1000, 3);
            using var server = new ValidatingServer(content, null, null);
            var repo = new InMemoryRepository();
            var transfer = NewDownload(path);
            repo.Insert(transfer);

            await RunToCompletion(CreateProcess(repo, server));

            Assert.Equal(2, server.Requests.Count);
            var resume = server.Requests.ElementAt(1);
            Assert.Equal(DropAfter, resume.RangeFrom);
            Assert.Null(resume.IfRange);
            Assert.Equal(HttpStatusCode.PartialContent, resume.Status);
            Assert.Equal(content, await File.ReadAllBytesAsync(path));
            Assert.Null(repo.Get<TransferValidator>(transfer.Identifier));
        }
        finally
        {
            TryDelete(path);
        }
    }


    [Fact(DisplayName = "Validator - removed when the download fails with an HTTP error")]
    public async Task Error_RemovesValidator()
    {
        var path = TempPath();
        try
        {
            using var server = new ValidatingServer(Bytes(1000, 3), "\"v1\"", null);
            var repo = new InMemoryRepository();
            var transfer = NewDownload(path);
            repo.Insert(transfer);
            server.OnRequest = n =>
            {
                if (n == 2)
                    server.NotFound = true;
            };

            await RunToCompletion(CreateProcess(repo, server));

            Assert.Null(repo.Get<HttpTransfer>(transfer.Identifier));
            Assert.Null(repo.Get<TransferValidator>(transfer.Identifier));
        }
        finally
        {
            TryDelete(path);
        }
    }


    [Fact(DisplayName = "Validator - removed when the transfer is cancelled")]
    public async Task Cancel_RemovesValidator()
    {
        var repo = new InMemoryRepository();
        var keep = NewDownload(TempPath());
        var cancel = NewDownload(TempPath());
        repo.Insert(keep);
        repo.Insert(cancel);
        repo.Set(new TransferValidator(keep.Identifier, "\"a\"", null));
        repo.Set(new TransferValidator(cancel.Identifier, "\"b\"", null));

        using var server = new ValidatingServer([], null, null);
        var manager = new HttpClientHttpTransferManager(
            CreateProcess(repo, server),
            NullLogger<HttpClientHttpTransferManager>.Instance,
            repo
        );
        await manager.Cancel(cancel.Identifier);

        Assert.Null(repo.Get<TransferValidator>(cancel.Identifier));
        Assert.NotNull(repo.Get<TransferValidator>(keep.Identifier));
    }


    [Fact(DisplayName = "Validator - all removed when every transfer is cancelled")]
    public async Task CancelAll_ClearsValidators()
    {
        var repo = new InMemoryRepository();
        var transfer = NewDownload(TempPath());
        repo.Insert(transfer);
        repo.Set(new TransferValidator(transfer.Identifier, "\"a\"", null));

        using var server = new ValidatingServer([], null, null);
        var manager = new HttpClientHttpTransferManager(
            CreateProcess(repo, server),
            NullLogger<HttpClientHttpTransferManager>.Instance,
            repo
        );
        await manager.CancelAll();

        Assert.Empty(repo.GetAll<TransferValidator>());
    }


    static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch { /* best effort */ }
    }


    record SeenRequest(long? RangeFrom, string? IfRange, DateTimeOffset? IfRangeDate, HttpStatusCode Status);


    /// <summary>
    /// In-memory file server that evaluates If-Range the way RFC 9110 says: a matching strong ETag or an exact
    /// Last-Modified date honors the Range (206), anything else sends the whole current file (200). The first
    /// response drops its connection after <see cref="DropAfter"/> bytes.
    /// </summary>
    sealed class ValidatingServer(byte[] content, string? etag, DateTimeOffset? lastModified) : HttpMessageHandler
    {
        int count;
        readonly ConcurrentQueue<SeenRequest> requests = new();

        public byte[] Content { get; set; } = content;
        public string? ETag { get; set; } = etag;
        public DateTimeOffset? LastModified { get; set; } = lastModified;
        public bool NotFound { get; set; }
        public Action<int>? OnRequest { get; set; }
        public IReadOnlyCollection<SeenRequest> Requests => this.requests;


        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var n = Interlocked.Increment(ref this.count);
            this.OnRequest?.Invoke(n);

            var from = request.Headers.Range?.Ranges.FirstOrDefault()?.From;
            var ifRange = request.Headers.IfRange;
            HttpResponseMessage response;

            if (this.NotFound)
            {
                response = new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new ByteArrayContent([]) };
            }
            else if (from != null && this.RangeConditionHolds(ifRange))
            {
                var rest = this.Content.Skip((int)from.Value).ToArray();
                response = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new ByteArrayContent(rest) };
                response.Content.Headers.ContentRange = new ContentRangeHeaderValue(from.Value, this.Content.Length - 1, this.Content.Length);
            }
            else
            {
                HttpContent body = n == 1
                    ? new StreamContent(new DroppingStream(this.Content, DropAfter))
                    : new ByteArrayContent(this.Content);
                body.Headers.ContentLength = this.Content.Length;
                response = new HttpResponseMessage(HttpStatusCode.OK) { Content = body };
            }

            if (this.ETag != null)
                response.Headers.TryAddWithoutValidation("ETag", this.ETag);
            if (this.LastModified != null)
                response.Content.Headers.LastModified = this.LastModified;

            this.requests.Enqueue(new SeenRequest(from, ifRange?.EntityTag?.Tag, ifRange?.Date, response.StatusCode));
            return Task.FromResult(response);
        }


        bool RangeConditionHolds(RangeConditionHeaderValue? ifRange)
        {
            if (ifRange == null)
                return true;

            if (ifRange.EntityTag != null)
            {
                var current = this.ETag;
                return !ifRange.EntityTag.IsWeak && current != null && !current.StartsWith("W/") && current == ifRange.EntityTag.Tag;
            }
            return ifRange.Date != null && ifRange.Date == this.LastModified;
        }
    }


    /// <summary>
    /// Serves the first <c>dropAfter</c> bytes, then fails like a dropped connection.
    /// </summary>
    sealed class DroppingStream(byte[] content, int dropAfter) : Stream
    {
        int position;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.position >= dropAfter)
                throw new IOException("Connection dropped");

            var length = Math.Min(count, dropAfter - this.position);
            Array.Copy(content, this.position, buffer, offset, length);
            this.position += length;
            return length;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => this.position; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
