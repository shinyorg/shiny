using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;

namespace Shiny.Net.Http;


public static class HttpClientExtensions
{
    public static IObservable<TransferProgress> Upload(
        this HttpClient httpClient,
        string uri,
        string filePath,        
        HttpMethod? httpMethod = null,
        HttpContent? bodyContent = null,
        string contenFormDataName = "value",
        string fileFormDataName = "file",
        params (string Name, string Value)[] headers
    ) => Observable.Create<TransferProgress>(ob => Observable.FromAsync(async ct =>
    {
        var file = new FileInfo(filePath);
        using var stream = file.OpenRead();

        var totalBytesXfer = 0L;
        var totalSince = 0L;

        var stop = new Stopwatch();

        using var progress = new ProgressStreamContent(
            stream,
            sent =>
            {
                totalBytesXfer += sent;
                totalSince += sent;

                if (totalBytesXfer == file.Length)
                {
                    ob.OnNext(new TransferProgress(
                        0,
                        file.Length,
                        file.Length
                    ));
                    stop.Stop();
                }
                else if (stop.Elapsed.TotalSeconds > 2)
                {
                    var bytesPerSecond = Convert.ToInt64(totalSince / stop.Elapsed.TotalSeconds);

                    ob.OnNext(new TransferProgress(
                        bytesPerSecond,
                        file.Length,
                        totalBytesXfer
                    ));

                    totalSince = 0;
                    stop.Restart();
                }
            },
            8192
        );
        var multipart = new MultipartFormDataContent();
        if (bodyContent != null)
            multipart.Add(bodyContent, contenFormDataName);

        multipart.Add(progress, name: fileFormDataName, fileName: file.Name);

        var request = new HttpRequestMessage();
        request.Content = multipart;
        request.Method = httpMethod ?? HttpMethod.Post;
        request.RequestUri = new Uri(uri);
        foreach (var header in headers)
            request.Headers.TryAddWithoutValidation(header.Name, header.Value);

        stop.Start();
        var response = await httpClient.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    })
    .Subscribe(
        _ => { },
        ob.OnError,
        ob.OnCompleted
    ));
    

    public static IObservable<TransferProgress> Download(
        this HttpClient httpClient,
        string uri,
        string toFilePath,
        int bufferSize = 8192,
        HttpMethod? httpMethod = null,
        HttpContent? bodyContent = null,
        params (string Name, string Value)[] headers
    ) => Observable.Create<TransferProgress>(ob => Observable.FromAsync(async ct =>
    {
        var request = new HttpRequestMessage();
        request.Method = httpMethod ?? HttpMethod.Get;
        request.RequestUri = new Uri(uri);
        request.Content = bodyContent;

        foreach (var header in headers)
            request.Headers.TryAddWithoutValidation(header.Name, header.Value);

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();        

        using var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var dest = File.Create(toFilePath);

        var contentLength = response.Content.Headers.ContentLength;
        var totalBytesXfer = 0L;
        var totalSince = 0L;
        var bytesRead = 0;
        var buffer = new byte[bufferSize];

        var stop = new Stopwatch();
        stop.Start();

        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) != 0)
        {
            await dest.WriteAsync(buffer, 0, bytesRead, ct).ConfigureAwait(false);
            totalSince += bytesRead;
            totalBytesXfer += bytesRead;

            if (totalBytesXfer == contentLength)
            {
                // completed
                stop.Stop();
                ob.OnNext(new TransferProgress(
                    0,
                    totalBytesXfer,
                    totalBytesXfer
                ));
            }
            else if (stop.Elapsed.TotalSeconds > 2)
            {
                var bytesPerSecond = Convert.ToInt32(totalSince / stop.Elapsed.TotalSeconds);
                ob.OnNext(new TransferProgress(
                    bytesPerSecond,
                    contentLength,
                    totalBytesXfer
                ));

                totalSince = 0;
                stop.Restart();
            }
        }
    })
    .Subscribe(
        _ => { },
        ob.OnError,
        ob.OnCompleted
    ));
}