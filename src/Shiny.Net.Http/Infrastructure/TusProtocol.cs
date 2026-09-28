using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Net.Http.Infrastructure;


// https://tus.io/protocols/resumable-upload - core protocol + creation extension
/// <summary>
/// The requests that make up a tus 1.0.0 upload: create the upload (<c>POST</c>), ask the server how much it
/// already has (<c>HEAD</c>), and send the rest (<c>PATCH</c>). Shared by the managed transfer loop and the
/// Apple background session.
/// </summary>
public static class TusProtocol
{
    /// <summary>The protocol version sent in every <c>Tus-Resumable</c> header.</summary>
    public const string Version = "1.0.0";

    /// <summary>The content type every <c>PATCH</c> body must carry.</summary>
    public const string OffsetContentType = "application/offset+octet-stream";

    public const string TusResumableHeader = "Tus-Resumable";
    public const string UploadOffsetHeader = "Upload-Offset";
    public const string UploadLengthHeader = "Upload-Length";
    public const string UploadMetadataHeader = "Upload-Metadata";


    /// <summary>
    /// Encodes metadata as the <c>Upload-Metadata</c> header value - comma-separated <c>key base64(value)</c> pairs.
    /// </summary>
    public static string EncodeMetadata(IDictionary<string, string> metadata)
    {
        foreach (var key in metadata.Keys)
        {
            if (String.IsNullOrWhiteSpace(key) || key.Contains(' ') || key.Contains(','))
                throw new ArgumentException($"Invalid tus metadata key '{key}' - keys must be non-empty and contain no spaces or commas");
        }

        return String.Join(
            ",",
            metadata.Select(x => String.IsNullOrEmpty(x.Value)
                ? x.Key
                : $"{x.Key} {Convert.ToBase64String(Encoding.UTF8.GetBytes(x.Value))}"
            )
        );
    }


    /// <summary>
    /// Creates the upload on the server and returns its absolute upload URL (the <c>Location</c> header,
    /// resolved against the creation endpoint when the server returns a relative path).
    /// </summary>
    public static async Task<string> Create(HttpClient httpClient, HttpTransferRequest request, long uploadLength, CancellationToken cancelToken)
    {
        using var httpReq = CreateRequest(HttpMethod.Post, request.Uri, request);
        httpReq.Headers.TryAddWithoutValidation(UploadLengthHeader, uploadLength.ToString(CultureInfo.InvariantCulture));
        if (request.TusMetadata?.Count > 0)
            httpReq.Headers.TryAddWithoutValidation(UploadMetadataHeader, EncodeMetadata(request.TusMetadata));

        // some servers reject a POST without a body length
        httpReq.Content = new ByteArrayContent([]);

        using var response = await httpClient.SendAsync(httpReq, cancelToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location;
        if (location == null)
            throw new HttpRequestException("tus server did not return a Location header for the created upload", null, response.StatusCode);

        return ResolveLocation(request.Uri, location);
    }


    /// <summary>
    /// Asks the server how many bytes of the upload it has. Returns null when the upload no longer exists
    /// (404, 410, or 403 - the server expired or refused it), in which case it has to be created again.
    /// </summary>
    public static async Task<long?> GetOffset(HttpClient httpClient, HttpTransferRequest request, string uploadUri, CancellationToken cancelToken)
    {
        using var httpReq = CreateRequest(HttpMethod.Head, uploadUri, request);
        using var response = await httpClient.SendAsync(httpReq, cancelToken).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone or HttpStatusCode.Forbidden)
            return null;

        response.EnsureSuccessStatusCode();
        return ReadOffset(response) ?? throw new HttpRequestException("tus server did not return an Upload-Offset header", null, response.StatusCode);
    }


    /// <summary>
    /// Sends <paramref name="count"/> bytes of the file starting at <paramref name="offset"/> and returns the
    /// server's new offset. Returns null when the server answers 409 Conflict (its offset is not the one we
    /// sent) - ask again with <see cref="GetOffset"/> and continue from there.
    /// </summary>
    public static async Task<long?> Patch(
        HttpClient httpClient,
        HttpTransferRequest request,
        string uploadUri,
        long offset,
        long count,
        Action<long>? onBytesSent,
        CancellationToken cancelToken
    )
    {
        using var httpReq = CreatePatchRequest(uploadUri, request, offset);
        httpReq.Content = new TusChunkContent(request.LocalFilePath, offset, count, onBytesSent);

        using var response = await httpClient.SendAsync(httpReq, cancelToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Conflict)
            return null;

        response.EnsureSuccessStatusCode();
        return ReadOffset(response) ?? offset + count;
    }


    /// <summary>
    /// Builds a <c>PATCH</c> request with the tus headers but no body - the Apple background session attaches the
    /// chunk as a file.
    /// </summary>
    public static HttpRequestMessage CreatePatchRequest(string uploadUri, HttpTransferRequest request, long offset)
    {
        var httpReq = CreateRequest(HttpMethod.Patch, uploadUri, request);
        httpReq.Headers.TryAddWithoutValidation(UploadOffsetHeader, offset.ToString(CultureInfo.InvariantCulture));
        return httpReq;
    }


    /// <summary>
    /// The number of bytes the next <c>PATCH</c> should carry from <paramref name="offset"/>.
    /// </summary>
    public static long GetChunkLength(HttpTransferRequest request, long offset, long uploadLength)
    {
        var remaining = uploadLength - offset;
        var chunk = request.TusChunkSize;
        return chunk is > 0 && chunk.Value < remaining ? chunk.Value : remaining;
    }


    public static long? ReadOffset(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues(UploadOffsetHeader, out var values) &&
            Int64.TryParse(values.FirstOrDefault(), NumberStyles.None, CultureInfo.InvariantCulture, out var offset))
            return offset;

        return null;
    }


    public static string ResolveLocation(string endpoint, Uri location)
        => location.IsAbsoluteUri
            ? location.ToString()
            : new Uri(new Uri(endpoint), location).ToString();


    static HttpRequestMessage CreateRequest(HttpMethod method, string uri, HttpTransferRequest request)
    {
        var httpReq = new HttpRequestMessage(method, uri);
        if (request.Headers != null)
        {
            foreach (var header in request.Headers)
                httpReq.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        httpReq.Headers.TryAddWithoutValidation(TusResumableHeader, Version);
        return httpReq;
    }
}


/// <summary>
/// A <c>PATCH</c> body that streams one slice of the file, honours cancellation between reads (so a pause
/// interrupts it promptly), and reports each block as it goes.
/// </summary>
sealed class TusChunkContent : HttpContent
{
    readonly string path;
    readonly long offset;
    readonly long count;
    readonly Action<long>? onBytesSent;

    public TusChunkContent(string path, long offset, long count, Action<long>? onBytesSent)
    {
        this.path = path;
        this.offset = offset;
        this.count = count;
        this.onBytesSent = onBytesSent;
        this.Headers.TryAddWithoutValidation("Content-Type", TusProtocol.OffsetContentType);
    }


    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => this.SerializeToStreamAsync(stream, context, CancellationToken.None);


    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        await using var file = new FileStream(this.path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        file.Position = this.offset;

        var buffer = new byte[81920];
        var remaining = this.count;
        while (remaining > 0)
        {
            var read = await file
                .ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), cancellationToken)
                .ConfigureAwait(false);

            if (read == 0)
                throw new IOException($"Upload file ended {remaining} bytes early - was it changed after the upload was queued?");

            await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            remaining -= read;
            this.onBytesSent?.Invoke(read);
        }
    }


    protected override bool TryComputeLength(out long length)
    {
        length = this.count;
        return true;
    }
}
