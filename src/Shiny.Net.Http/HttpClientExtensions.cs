using System;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;

namespace Shiny.Net.Http;


public record HttpTransferMetric2(
    double Progress, // 0.0 - 1.0
    long BytesPerSecond = 0,
    TimeSpan? EstimatedTimeRemaining = null
);


public static class HttpClientExtensions
{
    public static IObservable<HttpTransferMetric2> Download(this HttpClient httpClient, HttpRequestMessage request, string saveFullPath) => Observable.Create<HttpTransferMetric2>(async ob =>
    {
        var cts = new CancellationTokenSource();
        try
        {
            var dt = DateTimeOffset.UtcNow;

            var response = await httpClient
                .SendAsync(request, cts.Token)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            // TODO: resume?
            var buffer = new byte[8192];
            var total = 0;
            var stream = response.Content.ReadAsStream(cts.Token);
            var read = stream.Read(buffer, 0, buffer.Length);

            while (read > 0 && !cts.IsCancellationRequested)
            {
                // TODO: need to read file size from initial headers
                // TODO: write to file
                read = stream.Read(buffer, 0, buffer.Length);
                total += read;

                //var progress = total / file.Length;

                // TODO: throttle otherwise there will be a lot of updates
                // TODO: calculate time remaining
                // TODO: calculate bytes per second
                // TODO: calculate percentage complete (double 0.0 - 1.0)
                // TODO: calculate how much time is remaining based on bytes per second vs size remaining
            }

        }
        catch (Exception ex)
        {
            ob.OnError(ex);
        }
        return () =>
        {
            cts.Cancel();
        };
    });



    public static IObservable<HttpTransferMetric2> Upload(this HttpClient httpClient, HttpRequestMessage request, string uploadFullPath) => Observable.Create<HttpTransferMetric2>(async ob =>
    {
        var cts = new CancellationTokenSource();

        try
        {
            var total = 0;
            var file = new FileInfo(uploadFullPath);
            var progress = new Action<int>(sent =>
            {
                total += sent;
                var progress = total / file.Length;

                // TODO: throttle otherwise there will be a lot of updates
                // TODO: calculate time remaining
                // TODO: calculate bytes per second
                // TODO: calculate how much time is remaining based on bytes per second vs size remaining
            });

            request.Content = new ProgressStreamContent(file.OpenRead(), progress, 8192);
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            ob.OnError(ex);
        }
        return () =>
        {
            cts.Cancel();
        };
    });
}