using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public static class HttpClientExtensions
{
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
