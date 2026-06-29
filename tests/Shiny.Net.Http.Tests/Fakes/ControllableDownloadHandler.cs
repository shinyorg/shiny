using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Net.Http.Tests.Fakes;


/// <summary>
/// Fake <see cref="HttpMessageHandler"/> for download tests. The first (range-less) request
/// streams <c>pauseAt</c> bytes and then blocks until the request is cancelled - giving the
/// test a deterministic "mid-stream" window to pause in. A subsequent <c>Range</c> request
/// (the resume) serves the remaining bytes to completion, so the assembled file equals
/// <see cref="FullContent"/>.
/// </summary>
public sealed class ControllableDownloadHandler(byte[] fullContent, int pauseAt) : HttpMessageHandler
{
    public byte[] FullContent { get; } = fullContent;

    int rangeRequestCount;
    public int RangeRequestCount => Volatile.Read(ref this.rangeRequestCount);

    int totalRequestCount;
    public int TotalRequestCount => Volatile.Read(ref this.totalRequestCount);

    readonly TaskCompletionSource firstByteServed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes once the blocking first response has streamed its initial chunk.</summary>
    public Task FirstChunkServed => this.firstByteServed.Task;


    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref this.totalRequestCount);

        var range = request.Headers.Range;
        var hasRange = range?.Ranges.Count > 0;
        var from = 0;
        if (hasRange)
        {
            from = (int)(range!.Ranges.First().From ?? 0);
            Interlocked.Increment(ref this.rangeRequestCount);
        }

        HttpResponseMessage response;
        if (!hasRange)
        {
            // first attempt: serve [0, pauseAt) then block until cancelled (the pause)
            var segment = new byte[pauseAt];
            Array.Copy(this.FullContent, 0, segment, 0, pauseAt);

            var stream = new BlockAfterStream(segment, this.firstByteServed);
            response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream)
            };
            response.Content.Headers.ContentLength = this.FullContent.Length;
        }
        else
        {
            // resume: serve the remaining bytes from the requested offset to completion
            var remaining = this.FullContent.Length - from;
            var segment = new byte[remaining];
            Array.Copy(this.FullContent, from, segment, 0, remaining);

            response = new HttpResponseMessage(HttpStatusCode.PartialContent)
            {
                Content = new StreamContent(new BlockAfterStream(segment, null))
            };
            response.Content.Headers.ContentLength = remaining;
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(from, this.FullContent.Length - 1, this.FullContent.Length);
        }
        return Task.FromResult(response);
    }


    /// <summary>
    /// Streams a fixed byte segment, then either signals EOF or blocks forever (until the
    /// read's cancellation token fires) to emulate a slow/stalled server body.
    /// </summary>
    sealed class BlockAfterStream(byte[] data, TaskCompletionSource? servedSignal) : Stream
    {
        int position;
        bool signaled;

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (this.position < data.Length)
            {
                var toCopy = Math.Min(buffer.Length, data.Length - this.position);
                data.AsMemory(this.position, toCopy).CopyTo(buffer);
                this.position += toCopy;

                if (!this.signaled)
                {
                    this.signaled = true;
                    servedSignal?.TrySetResult();
                }
                return toCopy;
            }

            if (servedSignal == null)
                return 0; // resume stream: clean EOF

            // first stream: block until the transfer is cancelled (paused)
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            return 0;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => this.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override int Read(byte[] buffer, int offset, int count)
            => this.ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

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
