using System;
using System.Collections.Generic;
using System.Reactive.Linq;


namespace Shiny.Net.Http
{
    public static class MetricExtensions
    {
        public static IObservable<HttpTransferMetric> WithMetrics(this IObservable<HttpTransfer> transfers)
        {
            var startTime = DateTime.UtcNow;
            var metrics = new Dictionary<string, (DateTime LastPing, long LastBytesTransferred)>();

            return transfers.Select(transfer =>
            {
                //var metric = AddOrGet(transfer, metrics);
                //metric.BytesPerSecond = 0;
                //metric.EstimatedCompletionTime = TimeSpan.Zero;

                switch (transfer.Status)
                {
                    case HttpTransferState.InProgress:
                        //ApplyMetric(transfer, metric);
                        break;

                    case HttpTransferState.Cancelled:
                    case HttpTransferState.Completed:
                    case HttpTransferState.Error:
                    default:
                        lock (metrics)
                            metrics.Remove(transfer.Identifier);
                        break;
                }
                //return metric;
                return default(HttpTransferMetric);
            });
        }


        //static void ApplyMetric(HttpTransfer transfer, HttpTransferMetric metric)
        //{
        //    if (transfer.FileSize <= 0 || transfer.BytesTransferred <= 0)
        //        return;

        //    // I don't have a start to calc from yet
        //    if (metric.LastChanged == null)
        //    {
        //        metric.LastChanged = DateTime.UtcNow;
        //        metric.LastBytesTransferred = transfer.BytesTransferred;
        //    }
        //    else
        //    {
        //        var elapsed = DateTime.UtcNow - metric.LastChanged.Value;
        //        var totalSeconds = (long)elapsed.TotalSeconds;

        //        // sampling rate of 2 seconds
        //        if (totalSeconds >= 2)
        //        {
        //            var xferDiff = transfer.BytesTransferred - metric.LastBytesTransferred;
        //            var bytesPerSecond = xferDiff / totalSeconds;
        //            var rawEta = (transfer.FileSize - transfer.BytesTransferred) / bytesPerSecond;

        //            //Console.WriteLine($"b/s: {bytesPerSecond} - ETA: {rawEta}");
        //            //metric.BytesPerSecond = Convert.ToInt64(bytesPerSecond);
        //            //metric.EstimatedCompletionTime = TimeSpan.FromSeconds(rawEta);
        //            metric.LastChanged = DateTime.UtcNow;
        //            metric.LastBytesTransferred = transfer.BytesTransferred;
        //        }
        //    }
        //}


        //static HttpTransferMetrics AddOrGet(IHttpTransfer transfer, IDictionary<string, HttpTransferMetrics> metrics)
        //{
        //    lock (metrics)
        //    {
        //        HttpTransferMetrics metric = null;
        //        if (metrics.ContainsKey(transfer.Identifier))
        //            metric = metrics[transfer.Identifier];
        //        else
        //        {
        //            metric = new HttpTransferMetrics { Transfer = transfer };
        //            metrics.Add(transfer.Identifier, metric);
        //        }
        //        return metric;
        //    }
        //}

    }
}
