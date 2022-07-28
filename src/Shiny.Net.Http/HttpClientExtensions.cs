using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;

namespace Shiny.Net.Http;


public record HttpTransferMetric2(
    double Progress, // 0.0 - 1.0
    long? TotalBytes,
    long? BytesRemaining,
    long BytesPerSecond = 0,
    TimeSpan? EstimatedTimeRemaining = null,
    TimeSpan ElapsedTime
);


public static class HttpClientExtensions
{
    public static IObservable<HttpTransferMetric2> Download(this HttpClient httpClient, HttpRequestMessage request, string saveFullPath) => Observable.Create<HttpTransferMetric2>(async ob =>
    {
        var cts = new CancellationTokenSource();
        var sw = new Stopwatch();
        var startTime = DateTimeOffset.UtcNow;
        var buffer = new byte[8192];
        var total = 0;
        var oldTotal = 0;

        try
        {
            using var localFile = File.OpenWrite(saveFullPath); // TODO: could try resume download instead of recreating file?

            using var response = await httpClient
                .SendAsync(request, cts.Token)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            using var stream = response.Content.ReadAsStream(cts.Token);

            var dlSize = response.Content.Headers.ContentLength ?? 0;
            // TODO: if 0, progress is indeterministic

            sw.Start();
            var read = stream.Read(buffer, 0, buffer.Length);

            while (read > 0 && !cts.IsCancellationRequested)
            {
                read = stream.Read(buffer, 0, buffer.Length);
                localFile.Write(buffer, 0, buffer.Length);
                total += read;

                var elapsed = sw.Elapsed;
                if (elapsed.TotalMilliseconds >= 2000)
                {
                    sw.Reset();
                    sw.Start();

                    var bytesChanged = total - oldTotal;
                    oldTotal = total;

                    var bps = Convert.ToInt64(bytesChanged / elapsed.TotalSeconds);
                    var progress = total / dlSize;
                    var remaining = dlSize - total;
                    var estTimeRemaining = TimeSpan.FromSeconds(remaining / bps);

                    ob.OnNext(new HttpTransferMetric2(
                        progress,
                        dlSize,
                        remaining,
                        bps,
                        estTimeRemaining,
                        startTime.Subtract(DateTimeOffset.UtcNow)
                    ));
                }
            }

        }
        catch (Exception ex)
        {
            ob.OnError(ex);
        }
        return () =>
        {
            sw.Stop();
            cts.Cancel();
        };
    });



    public static IObservable<HttpTransferMetric2> Upload(this HttpClient httpClient, HttpRequestMessage request, string uploadFullPath) => Observable.Create<HttpTransferMetric2>(async ob =>
    {
        var cts = new CancellationTokenSource();
        var sw = new Stopwatch();

        try
        {
            var total = 0;
            var file = new FileInfo(uploadFullPath);
            sw.Start();

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
            sw.Stop();
        };
    });
}