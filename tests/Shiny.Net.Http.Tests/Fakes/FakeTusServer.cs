using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Net.Http.Tests.Fakes;


/// <summary>
/// In-memory tus 1.0.0 server (core + creation) behind an <see cref="HttpMessageHandler"/>. Keeps the bytes
/// of a PATCH that was cut short - as real tus servers do - so resumes can be checked against its offset.
/// </summary>
public sealed class FakeTusServer : HttpMessageHandler
{
    public const string Endpoint = "https://tus.local/files/";

    public sealed class Upload
    {
        public long Length { get; init; }
        public string? Metadata { get; init; }
        public MemoryStream Data { get; } = new();
    }

    public ConcurrentDictionary<string, Upload> Uploads { get; } = new();
    public ConcurrentQueue<string> Log { get; } = new();

    /// <summary>When set, the next PATCH keeps this many bytes and then fails as a dropped connection.</summary>
    public long? DropNextPatchAfter { get; set; }

    /// <summary>When set, the next PATCH keeps this many bytes and then stalls until the request is cancelled.</summary>
    public long? StallNextPatchAfter { get; set; }

    readonly TaskCompletionSource stalled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task PatchStalled => this.stalled.Task;

    public int Count(string method) => this.Log.Count(x => x.StartsWith(method + " "));


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath;
        this.Log.Enqueue($"{request.Method} {path}");

        if (Header(request, "Tus-Resumable") != "1.0.0")
            return new HttpResponseMessage(HttpStatusCode.PreconditionFailed);

        if (request.Method == HttpMethod.Post)
        {
            var id = Guid.NewGuid().ToString("N");
            this.Uploads[id] = new Upload
            {
                Length = Int64.Parse(Header(request, "Upload-Length")!),
                Metadata = Header(request, "Upload-Metadata")
            };
            var created = new HttpResponseMessage(HttpStatusCode.Created);
            created.Headers.Location = new Uri("/files/" + id, UriKind.Relative); // relative on purpose
            return created;
        }

        var upload = this.Uploads.GetValueOrDefault(path.Split('/').Last());
        if (upload == null)
            return new HttpResponseMessage(HttpStatusCode.NotFound);

        if (request.Method == HttpMethod.Head)
            return WithOffset(new HttpResponseMessage(HttpStatusCode.OK), upload);

        if (request.Method == HttpMethod.Patch)
        {
            if (request.Content?.Headers.ContentType?.MediaType != "application/offset+octet-stream")
                return new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType);

            if (Int64.Parse(Header(request, "Upload-Offset")!) != upload.Data.Length)
                return WithOffset(new HttpResponseMessage(HttpStatusCode.Conflict), upload);

            var drop = this.DropNextPatchAfter;
            var stall = this.StallNextPatchAfter;
            this.DropNextPatchAfter = null;
            this.StallNextPatchAfter = null;

            var sink = new SinkStream(upload.Data, drop ?? stall, drop != null, this.stalled);
            await request.Content.CopyToAsync(sink, cancellationToken).ConfigureAwait(false);
            return WithOffset(new HttpResponseMessage(HttpStatusCode.NoContent), upload);
        }
        return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed);
    }


    public static string DecodeMetadata(string header, string key)
    {
        var pair = header.Split(',').Select(x => x.Split(' ')).First(x => x[0] == key);
        return Encoding.UTF8.GetString(Convert.FromBase64String(pair[1]));
    }


    static HttpResponseMessage WithOffset(HttpResponseMessage response, Upload upload)
    {
        response.Headers.TryAddWithoutValidation("Upload-Offset", upload.Data.Length.ToString());
        response.Headers.TryAddWithoutValidation("Upload-Length", upload.Length.ToString());
        return response;
    }


    static string? Header(HttpRequestMessage request, string name)
        => request.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;


    /// <summary>Receives a PATCH body, optionally dropping or stalling after a byte limit.</summary>
    sealed class SinkStream(MemoryStream target, long? limit, bool drop, TaskCompletionSource stalled) : Stream
    {
        long received;

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var take = limit == null ? buffer.Length : (int)Math.Min(buffer.Length, limit.Value - this.received);
            if (take > 0)
            {
                lock (target)
                    target.Write(buffer.Span[..take]);
                this.received += take;
            }

            if (limit != null && this.received >= limit.Value)
            {
                if (drop)
                    throw new HttpRequestException("Connection reset by peer", new IOException("Connection reset"));

                stalled.TrySetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => this.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override void Write(byte[] buffer, int offset, int count)
            => this.WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => this.received; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
