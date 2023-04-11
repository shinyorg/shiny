using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public static class HttpClientExtensions
{
    public static IObservable<TransferProgress> Upload(
        this HttpClient httpClient,
        string uri,
        string filePath,
        HttpMethod? httpMethod = null,
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

                if (stop.Elapsed.TotalSeconds > 2)
                {
                    var bytesPerSecond = Convert.ToInt64(totalSince / stop.Elapsed.TotalSeconds);
                    var bytesRemaining = file.Length - totalBytesXfer;
                    var timeRemaining = TimeSpan.FromSeconds(bytesRemaining / bytesPerSecond);
                    var percent = Math.Round((double)totalBytesXfer / file.Length, 2);

                    ob.OnNext(new TransferProgress(                        
                        bytesPerSecond,
                        file.Length,
                        totalBytesXfer,
                        timeRemaining,
                        percent
                    ));

                    totalSince = 0;
                    stop.Restart();
                }
            },
            8192
        );
        var multipart = new MultipartFormDataContent();
        multipart.Add(progress, name: "file", fileName: file.Name);

        var request = new HttpRequestMessage();
        request.Content = multipart;
        request.Method = httpMethod ?? HttpMethod.Post;
        request.RequestUri = new Uri(uri);
        foreach (var header in headers)
            request.Headers.TryAddWithoutValidation(header.Name, header.Value);

        stop.Start();
        var response = await httpClient.SendAsync(request);
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
        params (string Name, string Value)[] headers
    ) => Observable.Create<TransferProgress>(ob => Observable.FromAsync(async ct =>
    {
        var request = new HttpRequestMessage();
        request.Method = httpMethod ?? HttpMethod.Get;
        request.RequestUri = new Uri(uri);
        foreach (var header in headers)
            request.Headers.TryAddWithoutValidation(header.Name, header.Value);

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var contentLength = response.Content.Headers.ContentLength;

        using var source = await response.Content.ReadAsStreamAsync();
        using var dest = File.Create(toFilePath);

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

            if (stop.Elapsed.TotalSeconds > 2)
            {
                var bytesPerSecond = Convert.ToInt32(totalSince / stop.Elapsed.TotalSeconds);

                if (contentLength == null)
                {
                    ob.OnNext(new TransferProgress(
                        bytesPerSecond,
                        0,
                        totalBytesXfer,
                        TimeSpan.Zero,
                        0
                    ));
                }
                else
                {
                    var bytesRemaining = contentLength.Value - totalBytesXfer;
                    var timeRemaining = TimeSpan.FromSeconds(bytesRemaining / bytesPerSecond);
                    var percent = Math.Round((double)totalBytesXfer / contentLength.Value, 2);

                    ob.OnNext(new TransferProgress(
                        bytesPerSecond,
                        contentLength.Value,
                        totalBytesXfer,
                        timeRemaining,
                        percent
                    ));
                }
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


    //public IObservable<HttpTransferMetrics> ListenToMetrics() =>
    //    this.WhenAnyProperty()
    //        .Select(x => (x.Object.BytesTransferred, x.Object.BytesToTransfer))
    //        .Buffer(TimeSpan.FromSeconds(2))
    //        .Select(results =>
    //        {
    //            var timeRemaining = TimeSpan.Zero;
    //            var bytesPerSecond = 0L;

    //            if (results.Count > 0)
    //            {
    //                // total bytes to transfer - all bytes transferred = delta
    //                // add all deltas over the past 2 seconds for total bytes xfer
    //                var totalBytes = results.Sum(x => x.BytesToTransfer - x.BytesTransferred);
    //                if (totalBytes > 0)
    //                {
    //                    bytesPerSecond = Convert.ToInt64((double)totalBytes / 2); // in two seconds

    //                    var remainingBytes = this.BytesToTransfer - this.BytesTransferred;
    //                    var secondsRemaining = remainingBytes / bytesPerSecond;
    //                    timeRemaining = TimeSpan.FromSeconds(secondsRemaining);
    //                }
    //            }
    //            return new HttpTransferMetrics(
    //                timeRemaining,
    //                bytesPerSecond,
    //                this.BytesToTransfer,
    //                this.BytesTransferred,
    //                this.PercentComplete,
    //                this.Status
    //            );
    //        });
}

