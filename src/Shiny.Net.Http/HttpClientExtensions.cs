using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


/// <summary>
/// In-process (non-background) upload and download helpers built on <see cref="HttpClient"/>
/// that emit <see cref="TransferProgress"/> updates.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Uploads a local file to the given URI, reporting progress as bytes are sent.
    /// </summary>
    /// <param name="httpClient">The HTTP client to send with.</param>
    /// <param name="uri">The destination URI.</param>
    /// <param name="filePath">Path to the local file to upload.</param>
    /// <param name="sendAsMultipart">When true, wraps the body in multipart/form-data.</param>
    /// <param name="httpMethod">Optional HTTP method; defaults to POST.</param>
    /// <param name="bodyContent">Optional additional content for the request body.</param>
    /// <param name="contenFormDataName">Form field name for body content when multipart.</param>
    /// <param name="fileFormDataName">Form field name for the file when multipart.</param>
    /// <param name="headers">Optional headers to attach to the request.</param>
    /// <param name="onProgress">Optional callback invoked with progress updates.</param>
    /// <param name="cancellationToken">Token used to cancel the upload.</param>
    public static async Task Upload(
        this HttpClient httpClient,
        string uri,
        string filePath,
        bool sendAsMultipart,
        HttpMethod? httpMethod = null,
        HttpContent? bodyContent = null,
        string contenFormDataName = "value",
        string fileFormDataName = "file",
        (string Name, string Value)[]? headers = null,
        Action<TransferProgress>? onProgress = null,
        CancellationToken cancellationToken = default
    )
    {
        var file = new FileInfo(filePath);
        var totalBytesXfer = 0L;
        var totalSince = 0L;
        var stop = Stopwatch.StartNew();

        using var progress = new ProgressStreamContent(
            file.OpenRead(),
            sent =>
            {
                totalBytesXfer += sent;
                totalSince += sent;

                if (totalBytesXfer == file.Length)
                {
                    onProgress?.Invoke(new TransferProgress(0, file.Length, file.Length));
                    stop.Stop();
                }
                else if (stop.Elapsed.TotalSeconds > 2)
                {
                    var bytesPerSecond = Convert.ToInt64(totalSince / stop.Elapsed.TotalSeconds);
                    onProgress?.Invoke(new TransferProgress(bytesPerSecond, file.Length, totalBytesXfer));
                    totalSince = 0;
                    stop.Restart();
                }
            },
            8192
        );

        var request = new HttpRequestMessage();
        if (sendAsMultipart)
        {
            var multipart = new MultipartFormDataContent();
            if (bodyContent != null)
                multipart.Add(bodyContent, contenFormDataName);
            multipart.Add(progress, name: fileFormDataName, fileName: file.Name);
            request.Content = multipart;
        }
        else
        {
            request.Content = progress;
        }

        request.Method = httpMethod ?? HttpMethod.Post;
        request.RequestUri = new Uri(uri);
        if (headers != null)
            foreach (var header in headers)
                request.Headers.TryAddWithoutValidation(header.Name, header.Value);

        var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }


    /// <summary>
    /// Downloads a remote resource to a local file, reporting progress as bytes are received.
    /// </summary>
    /// <param name="httpClient">The HTTP client to send with.</param>
    /// <param name="uri">The source URI.</param>
    /// <param name="toFilePath">The destination file path.</param>
    /// <param name="bufferSize">Read buffer size in bytes.</param>
    /// <param name="httpMethod">Optional HTTP method; defaults to GET.</param>
    /// <param name="bodyContent">Optional request body content.</param>
    /// <param name="headers">Optional headers to attach to the request.</param>
    /// <param name="onProgress">Optional callback invoked with progress updates.</param>
    /// <param name="cancellationToken">Token used to cancel the download.</param>
    public static async Task Download(
        this HttpClient httpClient,
        string uri,
        string toFilePath,
        int bufferSize = 8192,
        HttpMethod? httpMethod = null,
        HttpContent? bodyContent = null,
        (string Name, string Value)[]? headers = null,
        Action<TransferProgress>? onProgress = null,
        CancellationToken cancellationToken = default
    )
    {
        var request = new HttpRequestMessage();
        request.Method = httpMethod ?? HttpMethod.Get;
        request.RequestUri = new Uri(uri);
        request.Content = bodyContent;

        if (headers != null)
            foreach (var header in headers)
                request.Headers.TryAddWithoutValidation(header.Name, header.Value);

        using var response = await httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var dest = File.Create(toFilePath);

        var contentLength = response.Content.Headers.ContentLength;
        var totalBytesXfer = 0L;
        var totalSince = 0L;
        int bytesRead;
        var buffer = new byte[bufferSize];
        var stop = Stopwatch.StartNew();

        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
        {
            await dest.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            totalSince += bytesRead;
            totalBytesXfer += bytesRead;

            if (contentLength != null && totalBytesXfer == contentLength)
            {
                stop.Stop();
                onProgress?.Invoke(new TransferProgress(0, totalBytesXfer, totalBytesXfer));
            }
            else if (stop.Elapsed.TotalSeconds > 2)
            {
                var bytesPerSecond = Convert.ToInt32(totalSince / stop.Elapsed.TotalSeconds);
                onProgress?.Invoke(new TransferProgress(bytesPerSecond, contentLength, totalBytesXfer));
                totalSince = 0;
                stop.Restart();
            }
        }

        if (totalBytesXfer > 0 && !(contentLength.HasValue && totalBytesXfer == contentLength))
        {
            stop.Stop();
            onProgress?.Invoke(new TransferProgress(0, totalBytesXfer, totalBytesXfer));
        }
    }
}
