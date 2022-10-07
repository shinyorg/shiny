//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Net.Http;
//using System.Reactive.Linq;
//using System.Threading;

//namespace Shiny.Net.Http;


//public record HttpTransferProgress(
//    double Progress,
//    long BytesRemaining,
//    TimeSpan EstimatedTimeRemaing
//);


//public record HttpTransferMetrics(
//    long BytesTransferred,
//    long BytesPerSecond,
//    TimeSpan ElapsedTime,
//    HttpTransferProgress? Progress
//)
//{
//    public bool IsIndeterministic => this.Progress == null;
//}


//public static class HttpClientExtensions
//{
//    public static IObservable<HttpTransferMetrics> Download(this HttpClient httpClient, HttpRequestMessage request, string saveFullPath) => Observable.Create<HttpTransferMetrics>(async ob =>
//    {
//        var startTime = DateTimeOffset.UtcNow;
//        var cts = new CancellationTokenSource();
//        var sw = new Stopwatch();

//        var buffer = new byte[8192];
//        var bytesTransferred = 0;
//        var oldBytesTransferred = 0;

//        try
//        {
//            using var localFile = File.OpenWrite(saveFullPath); // TODO: could try resume download instead of recreating file?

//            using var response = await httpClient
//                .SendAsync(request, cts.Token)
//                .ConfigureAwait(false);

//            response.EnsureSuccessStatusCode();
//            using var stream = response.Content.ReadAsStream(cts.Token);

//            var contentLength = response.Content.Headers.ContentLength ?? 0;
//            sw.Start();
//            var read = stream.Read(buffer, 0, buffer.Length);

//            while (read > 0 && !cts.IsCancellationRequested)
//            {
//                read = stream.Read(buffer, 0, buffer.Length);
//                localFile.Write(buffer, 0, buffer.Length);
//                bytesTransferred += read;

//                var elapsed = sw.Elapsed;
//                if (elapsed.TotalMilliseconds >= 2000)
//                {
//                    var bytesChanged = bytesTransferred - oldBytesTransferred;
//                    oldBytesTransferred = bytesTransferred;
//                    var bps = Convert.ToInt64(bytesChanged / elapsed.TotalSeconds);

//                    ob.OnNext(new HttpTransferMetrics(
//                        bytesTransferred,
//                        bps,
//                        DateTimeOffset.UtcNow.Subtract(startTime),
//                        CalculateProgress(contentLength, bytesTransferred, bps)
//                    ));

//                    sw.Restart();
//                }
//            }

//        }
//        catch (Exception ex)
//        {
//            ob.OnError(ex);
//        }
//        return () =>
//        {
//            sw.Stop();
//            cts.Cancel();
//        };
//    });



//    public static IObservable<HttpTransferMetrics> Upload(this HttpClient httpClient, HttpRequestMessage request, string uploadFullPath) => Observable.Create<HttpTransferMetrics>(async ob =>
//    {
//        var startTime = DateTimeOffset.UtcNow;
//        var cts = new CancellationTokenSource();
//        var sw = new Stopwatch();

//        var bytesTransferred = 0;
//        var oldBytesTransferred = 0;

//        try
//        {
//            var total = 0;
//            var file = new FileInfo(uploadFullPath);
//            sw.Start();

//            var progress = new Action<int>(sent =>
//            {
//                bytesTransferred += sent;

//                if (sw.Elapsed.TotalMilliseconds > 2000)
//                {
//                    var bytesChanged = bytesTransferred - oldBytesTransferred;
//                    var bps = 0;
//                    var progress = total / file.Length;

//                    ob.OnNext(new HttpTransferMetrics(
//                        bytesTransferred,
//                        bps,
//                        DateTimeOffset.UtcNow.Subtract(startTime),
//                        CalculateProgress(file.Length, bytesTransferred, bps)
//                    ));
//                    sw.Restart();
//                }
//            });

//            request.Content = new ProgressStreamContent(file.OpenRead(), progress, 8192);
//            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
//            response.EnsureSuccessStatusCode();
//        }
//        catch (Exception ex)
//        {
//            ob.OnError(ex);
//        }
//        return () =>
//        {
//            cts.Cancel();
//            sw.Stop();
//        };
//    });


//    static HttpTransferProgress? CalculateProgress(long contentLength, long bytesTransferred, long bytesPerSecond)
//    {
//        if (contentLength == 0)
//            return null;

//        var value = (double)(bytesTransferred / contentLength);
//        var bytesRemaining = contentLength - bytesTransferred;
//        var estTimeRemaining = TimeSpan.FromSeconds(bytesRemaining / bytesPerSecond);

//        return new HttpTransferProgress(
//            value,
//            bytesRemaining,
//            estTimeRemaining
//        );
//    }
//}