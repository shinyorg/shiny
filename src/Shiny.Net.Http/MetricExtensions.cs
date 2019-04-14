using System;
using System.Collections.Generic;
using System.Reactive.Linq;


namespace Shiny.Net.Http
{
    class MetricHolder
    {
        public string Identifier { get; set; }
        public long LastBytesTransferred { get; set; }
        public DateTime LastPing { get; set; }
    }


    public static class MetricExtensions
    {
        public static IObservable<HttpTransferMetric> WithMetrics(this IObservable<HttpTransfer> transfers)
        {
            var metrics = new Dictionary<string, MetricHolder>();

            return transfers.Select(transfer =>
            {
                var metric = default(HttpTransferMetric);
                switch (transfer.Status)
                {
                    case HttpTransferState.InProgress:
                        metric = Calculate(transfer, metrics);
                        break;

                    case HttpTransferState.Cancelled:
                    case HttpTransferState.Completed:
                    case HttpTransferState.Error:
                    default:
                        lock (metrics)
                            metrics.Remove(transfer.Identifier);
                        break;
                }
                return metric;
            });
        }


        static HttpTransferMetric Calculate(HttpTransfer transfer, IDictionary<string, MetricHolder> metrics)
        {
            lock (metrics)
            {
                var metric = default(HttpTransferMetric);

                if (!metrics.ContainsKey(transfer.Identifier))
                {
                    metrics.Add(transfer.Identifier, new MetricHolder
                    {
                        LastBytesTransferred = transfer.BytesTransferred,
                        LastPing = DateTime.UtcNow
                    });
                }
                else if (transfer.BytesTransferred > 0 && transfer.FileSize > 0)
                {
                    var holder = metrics[transfer.Identifier];
                    var elapsed = DateTime.UtcNow - holder.LastPing;
                    var totalSeconds = (long)elapsed.TotalSeconds;

                    // sampling rate of 2 seconds
                    if (totalSeconds >= 2)
                    {
                        var xferDiff = transfer.BytesTransferred - holder.LastBytesTransferred;
                        var bytesPerSecond = xferDiff / totalSeconds;
                        var rawEta = (transfer.FileSize - transfer.BytesTransferred) / bytesPerSecond;

                        //Console.WriteLine($"b/s: {bytesPerSecond} - ETA: {rawEta}");
                        //metric.BytesPerSecond = Convert.ToInt64(bytesPerSecond);
                        //metric.EstimatedCompletionTime = TimeSpan.FromSeconds(rawEta);
                        metric = new HttpTransferMetric(transfer, bytesPerSecond, TimeSpan.FromSeconds(rawEta));
                    }
                    holder.LastPing = DateTime.UtcNow;
                    holder.LastBytesTransferred = transfer.BytesTransferred;
                }
                return metric;
            }
        }
    }
}
